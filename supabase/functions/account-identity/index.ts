import { serve } from "https://deno.land/std@0.224.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.4";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

type RequestBody = {
  action?: string;
  username?: string;
  email?: string;
  password?: string;
  display_name?: string;
  application_user_id?: string;
  player_id?: string;
  otp?: string;
};

serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });

  try {
    if (req.method !== "POST") return json({ success: false, message: "Method not allowed" }, 405);

    const body = await req.json() as RequestBody;
    const action = normalize(body.action);
    const admin = createClient(mustEnv("SUPABASE_URL"), mustEnv("SUPABASE_SERVICE_ROLE_KEY"), {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    if (action === "check_username") {
      const username = cleanUsername(body.username);
      const validation = validateUsername(username);
      if (!validation.valid) return json({ success: false, available: false, message: validation.message }, 400);

      const available = await isUsernameAvailable(admin, username);
      return json({
        success: true,
        available,
        username,
        message: available ? "اسم المستخدم متاح." : "اسم المستخدم محجوز، اختر اسمًا آخر.",
      });
    }

    if (action === "suggest_username") {
      const base = sanitizeUsernameBase(body.username);
      const suggestion = await generateAvailableUsername(admin, base);
      return json({ success: true, available: true, username: suggestion, message: "تم إنشاء اسم مستخدم متاح." });
    }

    if (action === "register_account") {
      const username = cleanUsername(body.username);
      const email = normalizeEmail(body.email);
      const password = body.password ?? "";
      const displayName = (body.display_name ?? "").trim();

      const validation = validateUsername(username);
      if (!validation.valid) return json({ success: false, message: validation.message }, 400);
      if (!isValidEmail(email)) return json({ success: false, message: "البريد الإلكتروني غير صالح." }, 400);
      if (!isStrongPassword(password)) return json({ success: false, message: "كلمة المرور لا تطابق شروط القوة." }, 400);
      if (!displayName || displayName.length > 40) return json({ success: false, message: "الاسم الظاهر مطلوب ويجب ألا يتجاوز 40 حرفًا." }, 400);

      if (!await isUsernameAvailable(admin, username)) {
        return json({ success: false, code: "username_taken", message: "اسم المستخدم محجوز، اختر اسمًا آخر." }, 409);
      }

      const { data: existingIdentity } = await admin
        .from("account_identity_registry")
        .select("id")
        .eq("normalized_email", email)
        .maybeSingle();

      if (existingIdentity) return json({ success: false, code: "email_taken", message: "يوجد حساب مسجل مسبقًا بهذا البريد الإلكتروني." }, 409);

      const { data: created, error: createError } = await admin.auth.admin.createUser({
        email,
        password,
        email_confirm: true,
        user_metadata: {
          username,
          nickname: displayName,
          display_name: displayName,
          domino_email_verified: false,
        },
      });

      if (createError || !created.user) {
        console.error("account-identity:create_user_failed", createError);
        const message = translateAuthError(createError?.message ?? "تعذر إنشاء الحساب.");
        return json({ success: false, message }, 400);
      }

      const { error: registryError } = await admin
        .from("account_identity_registry")
        .insert({
          supabase_user_id: created.user.id,
          username,
          normalized_username: normalize(username),
          email,
          normalized_email: email,
          display_name: displayName,
          email_verified: false,
        });

      if (registryError) {
        console.error("account-identity:registry_insert_failed", registryError);
        await admin.auth.admin.deleteUser(created.user.id);
        return json({ success: false, message: "تعذر حجز اسم المستخدم. حاول باسم آخر." }, 409);
      }

      return json({
        success: true,
        user_id: created.user.id,
        username,
        email,
        email_verified: false,
        message: "تم إنشاء الحساب ويمكنك الدخول الآن. توثيق البريد متاح من الإعدادات.",
      });
    }

    if (action === "sync_player_identity") {
      const user = await requireAuthenticatedUser(admin, req);
      if (!user) return json({ success: false, message: "الجلسة غير صالحة." }, 401);

      const applicationUserId = (body.application_user_id ?? "").trim();
      const playerId = (body.player_id ?? "").trim();
      if (!applicationUserId || !playerId) return json({ success: false, message: "ApplicationUserId وPlayerId مطلوبان." }, 400);

      const { error } = await admin
        .from("account_identity_registry")
        .update({ application_user_id: applicationUserId, player_id: playerId, updated_at: new Date().toISOString() })
        .eq("supabase_user_id", user.id);

      if (error) return json({ success: false, message: "تعذر مزامنة هوية اللاعب." }, 500);
      return json({ success: true, message: "تم ربط هوية اللاعب بالحساب." });
    }

    if (action === "request_email_verification_otp") {
      const user = await requireAuthenticatedUser(admin, req);
      if (!user) return json({ success: false, message: "الجلسة غير صالحة." }, 401);

      const { data: identity, error: identityError } = await admin
        .from("account_identity_registry")
        .select("normalized_email, email_verified, username")
        .eq("supabase_user_id", user.id)
        .single();

      if (identityError || !identity) return json({ success: false, message: "تعذر تحميل هوية الحساب." }, 404);
      if (identity.email_verified) return json({ success: true, verified: true, message: "البريد الإلكتروني موثّق مسبقًا." });

      const { data: recent } = await admin
        .from("account_email_verification_otps")
        .select("created_at")
        .eq("supabase_user_id", user.id)
        .order("created_at", { ascending: false })
        .limit(1);

      if (recent?.[0]) {
        const elapsed = Date.now() - new Date(recent[0].created_at).getTime();
        const waitMs = 60_000 - elapsed;
        if (waitMs > 0) {
          return json({ success: false, retry_after_seconds: Math.ceil(waitMs / 1000), message: `انتظر ${Math.ceil(waitMs / 1000)} ثانية قبل طلب رمز جديد.` }, 429);
        }
      }

      const otp = generateOtp();
      const otpHash = await sha256(`${user.id}:${identity.normalized_email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const expiresAt = new Date(Date.now() + 10 * 60 * 1000).toISOString();

      await admin
        .from("account_email_verification_otps")
        .update({ consumed_at: new Date().toISOString() })
        .eq("supabase_user_id", user.id)
        .is("consumed_at", null);

      const { error: insertError } = await admin.from("account_email_verification_otps").insert({
        supabase_user_id: user.id,
        normalized_email: identity.normalized_email,
        otp_hash: otpHash,
        expires_at: expiresAt,
      });

      if (insertError) return json({ success: false, message: "تعذر إنشاء رمز التوثيق." }, 500);

      const emailResult = await sendVerificationEmail(identity.normalized_email, identity.username, otp);
      if (!emailResult.success) return json({ success: false, message: emailResult.message }, 500);

      return json({ success: true, expires_in_seconds: 600, retry_after_seconds: 60, message: "تم إرسال رمز توثيق البريد الإلكتروني." });
    }

    if (action === "verify_email_verification_otp") {
      const user = await requireAuthenticatedUser(admin, req);
      if (!user) return json({ success: false, message: "الجلسة غير صالحة." }, 401);

      const otp = (body.otp ?? "").trim();
      if (!/^\d{6}$/.test(otp)) return json({ success: false, message: "أدخل رمزًا صحيحًا من 6 أرقام." }, 400);

      const { data: identity } = await admin
        .from("account_identity_registry")
        .select("normalized_email")
        .eq("supabase_user_id", user.id)
        .single();
      if (!identity) return json({ success: false, message: "تعذر تحميل هوية الحساب." }, 404);

      const otpHash = await sha256(`${user.id}:${identity.normalized_email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const { data: rows } = await admin
        .from("account_email_verification_otps")
        .select("id, attempts, max_attempts, expires_at, consumed_at")
        .eq("supabase_user_id", user.id)
        .eq("otp_hash", otpHash)
        .order("created_at", { ascending: false })
        .limit(1);

      const row = rows?.[0];
      if (!row || row.consumed_at) return json({ success: false, message: "رمز التوثيق غير صحيح أو مستخدم مسبقًا." }, 400);
      if (new Date(row.expires_at).getTime() < Date.now()) return json({ success: false, message: "انتهت صلاحية رمز التوثيق." }, 400);
      if ((row.attempts ?? 0) >= (row.max_attempts ?? 5)) return json({ success: false, message: "تم تجاوز عدد المحاولات المسموح." }, 429);

      await admin.from("account_email_verification_otps").update({ attempts: (row.attempts ?? 0) + 1 }).eq("id", row.id);

      const now = new Date().toISOString();
      const { error: verifyError } = await admin
        .from("account_identity_registry")
        .update({ email_verified: true, email_verified_at: now, updated_at: now })
        .eq("supabase_user_id", user.id);

      if (verifyError) return json({ success: false, message: "تعذر حفظ حالة توثيق البريد." }, 500);

      await admin.from("account_email_verification_otps").update({ consumed_at: now }).eq("id", row.id);
      await admin.auth.admin.updateUserById(user.id, { user_metadata: { ...user.user_metadata, domino_email_verified: true } });

      return json({ success: true, verified: true, message: "تم توثيق البريد الإلكتروني بنجاح." });
    }

    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (error) {
    console.error("account-identity:unhandled", error);
    return json({ success: false, message: "تعذر تنفيذ عملية الهوية." }, 500);
  }
});

async function requireAuthenticatedUser(admin: any, req: Request) {
  const authorization = req.headers.get("authorization") ?? "";
  const token = authorization.replace(/^Bearer\s+/i, "").trim();
  if (!token || token.startsWith("sb_")) return null;
  const { data, error } = await admin.auth.getUser(token);
  return error ? null : data.user;
}

async function isUsernameAvailable(admin: any, username: string) {
  const { data, error } = await admin
    .from("account_identity_registry")
    .select("id")
    .eq("normalized_username", normalize(username))
    .maybeSingle();
  if (error) throw error;
  return !data;
}

async function generateAvailableUsername(admin: any, source: string) {
  const base = source || "Player";
  if (validateUsername(base).valid && await isUsernameAvailable(admin, base)) return base;

  const separators = ["_", ".", "-"];
  for (let attempt = 0; attempt < 50; attempt++) {
    const separator = separators[attempt % separators.length];
    const suffix = String(10 + crypto.getRandomValues(new Uint32Array(1))[0] % 9990);
    const maxBaseLength = Math.max(3, 32 - separator.length - suffix.length);
    const candidate = `${base.slice(0, maxBaseLength)}${separator}${suffix}`;
    if (validateUsername(candidate).valid && await isUsernameAvailable(admin, candidate)) return candidate;
  }

  return `Player_${crypto.randomUUID().replaceAll("-", "").slice(0, 8)}`;
}

function sanitizeUsernameBase(value?: string) {
  let result = (value ?? "Player").trim().replace(/[^\p{L}\p{N}_.-]+/gu, "");
  result = result.replace(/^[_.-]+|[_.-]+$/g, "");
  if (result.length < 3) result = "Player";
  return result.slice(0, 24);
}

function cleanUsername(value?: string) {
  return (value ?? "").trim();
}

function validateUsername(username: string) {
  if (username.length < 3 || username.length > 32) return { valid: false, message: "اسم المستخدم يجب أن يكون بين 3 و32 حرفًا." };
  if (username.includes("@")) return { valid: false, message: "لا يمكن استخدام @ داخل اسم المستخدم." };
  if (!/^[\p{L}\p{N}_.-]+$/u.test(username)) return { valid: false, message: "يسمح بالحروف والأرقام والرموز . _ - فقط." };
  if (/^[_.-]|[_.-]$/.test(username)) return { valid: false, message: "لا يمكن أن يبدأ أو ينتهي اسم المستخدم برمز." };
  return { valid: true, message: "" };
}

function normalize(value?: string) { return (value ?? "").trim().toLowerCase(); }
function normalizeEmail(value?: string) { return normalize(value); }
function isValidEmail(value: string) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value); }
function isStrongPassword(value: string) { return value.length >= 8 && /[a-z]/.test(value) && /[A-Z]/.test(value) && /\d/.test(value) && /[^a-zA-Z0-9]/.test(value); }
function generateOtp() { const a = new Uint32Array(1); crypto.getRandomValues(a); return String(a[0] % 1_000_000).padStart(6, "0"); }
async function sha256(value: string) { const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value)); return Array.from(new Uint8Array(digest)).map((b) => b.toString(16).padStart(2, "0")).join(""); }
function mustEnv(name: string) { const value = Deno.env.get(name)?.trim(); if (!value) throw new Error(`Missing env ${name}`); return value; }

async function sendVerificationEmail(email: string, username: string, otp: string) {
  const apiKey = Deno.env.get("RESEND_API_KEY")?.trim() ?? "";
  const from = Deno.env.get("ACCOUNT_RECOVERY_FROM_EMAIL")?.trim() ?? "";
  if (!apiKey || !from) return { success: false, message: "إعدادات إرسال البريد غير مكتملة." };

  const response = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: { Authorization: `Bearer ${apiKey}`, "Content-Type": "application/json" },
    body: JSON.stringify({
      from,
      to: email,
      subject: "Domino Majlis PRO - رمز توثيق البريد",
      html: `<div dir="rtl" style="font-family:Arial;background:#070707;color:#fff;padding:28px;border-radius:18px"><h2 style="color:#d4af37">Domino Majlis PRO</h2><p>مرحبًا ${escapeHtml(username)}</p><p>رمز توثيق البريد الإلكتروني:</p><div style="font-size:32px;font-weight:bold;letter-spacing:8px;color:#d4af37">${otp}</div><p>ينتهي الرمز خلال 10 دقائق.</p></div>`,
    }),
  });

  if (!response.ok) {
    const text = await response.text();
    console.error("account-identity:resend_failed", response.status, text);
    return { success: false, message: translateResendError(response.status, text) };
  }
  return { success: true, message: "تم الإرسال." };
}

function translateAuthError(message: string) {
  if (/already.*registered|already exists/i.test(message)) return "يوجد حساب مسجل مسبقًا بهذا البريد الإلكتروني.";
  if (/rate limit/i.test(message)) return "تم تنفيذ محاولات كثيرة خلال وقت قصير. انتظر قليلًا ثم حاول مرة أخرى.";
  return message;
}

function translateResendError(status: number, body: string) {
  if (status === 429) return "تم إرسال عدة رموز خلال وقت قصير. انتظر قبل إعادة المحاولة.";
  if (status === 403) return "خدمة البريد في وضع الاختبار ولا تسمح بالإرسال إلى هذا العنوان حاليًا.";
  if (status === 401) return "مفتاح خدمة البريد غير صالح.";
  return `تعذر إرسال رمز التوثيق (${status}).`;
}

function escapeHtml(value: string) { return value.replace(/[&<>'"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[c] ?? c)); }
function json(payload: unknown, status = 200) { return new Response(JSON.stringify(payload), { status, headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" } }); }
