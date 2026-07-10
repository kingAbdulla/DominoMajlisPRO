import { serve } from "https://deno.land/std@0.224.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.4";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

type SecurityQuestionPayload = { question?: string; answer?: string };
type SecurityAnswerPayload = { question_id?: string; answer?: string };

type RequestBody = {
  action?: string;
  username?: string;
  email?: string;
  otp?: string;
  new_password?: string;
  questions?: SecurityQuestionPayload[];
  answers?: SecurityAnswerPayload[];
  challenge_token?: string;
  reset_token?: string;
};

serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });

  try {
    if (req.method !== "POST") return json({ success: false, message: "Method not allowed" }, 405);

    const body = await req.json() as RequestBody;
    const action = normalize(body.action);
    const username = normalize(body.username);
    const email = normalizeEmail(body.email);

    if (!username || !email) {
      return json({ success: false, message: "اسم المستخدم والبريد الإلكتروني مطلوبان." }, 400);
    }

    const admin = createClient(mustEnv("SUPABASE_URL"), mustEnv("SUPABASE_SERVICE_ROLE_KEY"), {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    const user = await findUserByUsernameAndEmail(admin, username, email);
    if (!user) {
      return json({ success: false, message: "تعذر التحقق من هوية الحساب." }, 400);
    }

    if (action === "register_security_questions") {
      const normalizedQuestions = normalizeQuestions(body.questions);
      if (normalizedQuestions.length !== 3) {
        return json({ success: false, message: "يجب تسجيل 3 أسئلة أمان مختلفة مع إجاباتها." }, 400);
      }

      const pepper = mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER");
      const rows = await Promise.all(normalizedQuestions.map(async (item) => ({
        user_id: user.id,
        username,
        email,
        question: item.question,
        answer_hash: await sha256(`${username}:${email}:${item.question}:${item.answer}:${pepper}`),
        updated_at: new Date().toISOString(),
      })));

      const { error } = await admin
        .from("account_security_questions")
        .upsert(rows, { onConflict: "username,email,question" });

      if (error) return json({ success: false, message: `تعذر حفظ أسئلة الأمان: ${error.message}` }, 500);
      return json({ success: true, message: "تم حفظ أسئلة الأمان بنجاح." });
    }

    if (action === "get_security_questions") {
      const { data: questions, error } = await admin
        .from("account_security_questions")
        .select("id, question")
        .eq("user_id", user.id)
        .eq("username", username)
        .eq("email", email)
        .order("created_at", { ascending: true });

      if (error) return json({ success: false, message: `تعذر تحميل أسئلة الأمان: ${error.message}` }, 500);
      if (!questions || questions.length !== 3) {
        return json({ success: false, message: "لا توجد 3 أسئلة أمان مسجلة لهذا الحساب." }, 400);
      }

      const challengeToken = randomToken();
      const challengeHash = await sha256(challengeToken);
      const expiresAt = new Date(Date.now() + 10 * 60 * 1000).toISOString();

      await admin
        .from("account_recovery_challenges")
        .update({ consumed_at: new Date().toISOString() })
        .eq("user_id", user.id)
        .is("consumed_at", null);

      const { error: insertError } = await admin
        .from("account_recovery_challenges")
        .insert({
          user_id: user.id,
          username,
          email,
          challenge_token_hash: challengeHash,
          expires_at: expiresAt,
        });

      if (insertError) return json({ success: false, message: `تعذر بدء التحقق: ${insertError.message}` }, 500);

      return json({
        success: true,
        message: "تم تحميل أسئلة الأمان المسجلة.",
        challenge_token: challengeToken,
        questions: questions.map((item: any) => ({ id: item.id, question: item.question })),
      });
    }

    if (action === "verify_security_answers") {
      const challengeToken = body.challenge_token?.trim() ?? "";
      const answers = normalizeAnswers(body.answers);
      if (!challengeToken || answers.length !== 3) {
        return json({ success: false, message: "بيانات التحقق غير مكتملة." }, 400);
      }

      const challengeHash = await sha256(challengeToken);
      const { data: challenges, error } = await admin
        .from("account_recovery_challenges")
        .select("id, user_id, attempts, max_attempts, expires_at, consumed_at")
        .eq("challenge_token_hash", challengeHash)
        .eq("username", username)
        .eq("email", email)
        .limit(1);

      if (error) return json({ success: false, message: "تعذر التحقق من الجلسة الأمنية." }, 500);
      const challenge = challenges?.[0];
      if (!challenge || challenge.consumed_at || new Date(challenge.expires_at).getTime() < Date.now()) {
        return json({ success: false, message: "انتهت جلسة التحقق. ابدأ من جديد." }, 400);
      }
      if ((challenge.attempts ?? 0) >= (challenge.max_attempts ?? 5)) {
        return json({ success: false, message: "تم تجاوز عدد المحاولات المسموح." }, 429);
      }

      const ids = answers.map((item) => item.questionId);
      const { data: stored, error: storedError } = await admin
        .from("account_security_questions")
        .select("id, question, answer_hash")
        .eq("user_id", user.id)
        .in("id", ids);

      if (storedError) return json({ success: false, message: "تعذر التحقق من الإجابات." }, 500);

      const pepper = mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER");
      const storedMap = new Map((stored ?? []).map((row: any) => [row.id, row]));
      let allMatch = storedMap.size === 3;

      for (const answer of answers) {
        const row: any = storedMap.get(answer.questionId);
        if (!row) { allMatch = false; break; }
        const hash = await sha256(`${username}:${email}:${normalize(row.question)}:${answer.answer}:${pepper}`);
        if (hash !== row.answer_hash) { allMatch = false; break; }
      }

      if (!allMatch) {
        await admin.from("account_recovery_challenges")
          .update({ attempts: (challenge.attempts ?? 0) + 1 })
          .eq("id", challenge.id);
        return json({ success: false, message: "إحدى إجابات أسئلة الأمان غير صحيحة." }, 400);
      }

      const resetToken = randomToken();
      const resetHash = await sha256(resetToken);
      await admin.from("account_recovery_challenges")
        .update({ reset_token_hash: resetHash, verified_at: new Date().toISOString() })
        .eq("id", challenge.id);

      return json({ success: true, message: "تم التحقق من الهوية بنجاح.", reset_token: resetToken });
    }

    if (action === "reset_password_with_security_token") {
      const resetToken = body.reset_token?.trim() ?? "";
      const newPassword = body.new_password ?? "";
      if (!resetToken || !isStrongPassword(newPassword)) {
        return json({ success: false, message: "رمز إعادة التعيين أو كلمة المرور الجديدة غير صالح." }, 400);
      }

      const resetHash = await sha256(resetToken);
      const { data: challenges, error } = await admin
        .from("account_recovery_challenges")
        .select("id, user_id, expires_at, verified_at, consumed_at")
        .eq("reset_token_hash", resetHash)
        .eq("username", username)
        .eq("email", email)
        .limit(1);

      if (error) return json({ success: false, message: "تعذر التحقق من رمز إعادة التعيين." }, 500);
      const challenge = challenges?.[0];
      if (!challenge || !challenge.verified_at || challenge.consumed_at || new Date(challenge.expires_at).getTime() < Date.now()) {
        return json({ success: false, message: "انتهت صلاحية التحقق. ابدأ عملية الاسترداد من جديد." }, 400);
      }

      const { error: updateError } = await admin.auth.admin.updateUserById(user.id, { password: newPassword });
      if (updateError) return json({ success: false, message: `تعذر تحديث كلمة المرور: ${updateError.message}` }, 500);

      const now = new Date().toISOString();
      await admin.from("account_recovery_challenges").update({ consumed_at: now }).eq("id", challenge.id);
      await admin.from("account_recovery_otps").update({ consumed_at: now }).eq("username", username).eq("email", email).is("consumed_at", null);

      return json({ success: true, message: "تم تحديث كلمة المرور بنجاح. سجّل الدخول بكلمة المرور الجديدة." });
    }

    if (action === "request_email_otp") {
      const otp = generateOtp();
      const otpHash = await sha256(`${username}:${email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const expiresAt = new Date(Date.now() + 10 * 60 * 1000).toISOString();
      const { error } = await admin.from("account_recovery_otps").insert({ username, email, otp_hash: otpHash, purpose: "password_reset", expires_at: expiresAt });
      if (error) return json({ success: false, message: `تعذر إنشاء رمز الاسترداد: ${error.message}` }, 500);
      const emailResult = await sendOtpEmail(email, username, otp);
      if (!emailResult.success) return json({ success: false, message: emailResult.message }, 500);
      return json({ success: true, message: "تم إرسال رمز التحقق إلى البريد الإلكتروني." });
    }

    if (action === "verify_email_otp_reset") {
      const otp = normalize(body.otp);
      const newPassword = body.new_password ?? "";
      if (!isStrongPassword(newPassword) || otp.length !== 6) return json({ success: false, message: "رمز التحقق أو كلمة المرور الجديدة غير صالح." }, 400);

      const otpHash = await sha256(`${username}:${email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const { data: rows, error } = await admin.from("account_recovery_otps")
        .select("id, attempts, max_attempts, expires_at, consumed_at")
        .eq("username", username).eq("email", email).eq("otp_hash", otpHash)
        .eq("purpose", "password_reset").order("created_at", { ascending: false }).limit(1);
      if (error) return json({ success: false, message: "تعذر التحقق من الرمز." }, 500);
      const row = rows?.[0];
      if (!row || row.consumed_at || new Date(row.expires_at).getTime() < Date.now()) return json({ success: false, message: "رمز التحقق غير صالح أو منتهي." }, 400);
      if ((row.attempts ?? 0) >= (row.max_attempts ?? 5)) return json({ success: false, message: "تم تجاوز عدد المحاولات المسموح." }, 429);

      await admin.from("account_recovery_otps").update({ attempts: (row.attempts ?? 0) + 1 }).eq("id", row.id);
      const { error: updateError } = await admin.auth.admin.updateUserById(user.id, { password: newPassword });
      if (updateError) return json({ success: false, message: `تعذر تحديث كلمة المرور: ${updateError.message}` }, 500);
      await admin.from("account_recovery_otps").update({ consumed_at: new Date().toISOString() }).eq("username", username).eq("email", email).is("consumed_at", null);
      return json({ success: true, message: "تم تحديث كلمة المرور بنجاح." });
    }

    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (err) {
    console.error("account-recovery:unhandled_error", err);
    return json({ success: false, message: `تعذر تنفيذ عملية الاسترداد: ${errorMessage(err)}` }, 500);
  }
});

async function findUserByUsernameAndEmail(admin: any, username: string, email: string) {
  let page = 1;
  while (page <= 20) {
    const { data, error } = await admin.auth.admin.listUsers({ page, perPage: 200 });
    if (error) throw error;
    const user = data?.users?.find((item: any) => {
      const metadataUsername = normalize(item?.user_metadata?.username);
      const metadataNickname = normalize(item?.user_metadata?.nickname);
      const metadataDisplayName = normalize(item?.user_metadata?.display_name);
      return (metadataUsername === username || metadataNickname === username || metadataDisplayName === username) && normalizeEmail(item?.email) === email;
    });
    if (user) return user;
    if (!data?.users || data.users.length < 200) return null;
    page++;
  }
  return null;
}

async function sendOtpEmail(email: string, username: string, otp: string) {
  const resendApiKey = Deno.env.get("RESEND_API_KEY")?.trim() ?? "";
  const fromEmail = Deno.env.get("ACCOUNT_RECOVERY_FROM_EMAIL")?.trim() ?? "";
  if (!resendApiKey || !fromEmail) return { success: false, message: "إعدادات البريد غير مكتملة." };

  const response = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: { "Authorization": `Bearer ${resendApiKey}`, "Content-Type": "application/json" },
    body: JSON.stringify({
      from: fromEmail,
      to: email,
      subject: "Domino Majlis PRO - رمز استعادة الحساب",
      html: `<div dir="rtl" style="font-family:Arial;background:#050505;color:#fff;padding:24px;border-radius:16px"><h2 style="color:#D4AF37">Domino Majlis PRO</h2><p>رمز استعادة الحساب:</p><div style="font-size:28px;font-weight:bold;letter-spacing:6px;color:#D4AF37">${otp}</div><p>ينتهي خلال 10 دقائق.</p></div>`,
    }),
  });
  if (!response.ok) return { success: false, message: `فشل إرسال البريد عبر Resend: ${response.status} - ${await response.text()}` };
  return { success: true, message: "تم الإرسال." };
}

function normalizeQuestions(items?: SecurityQuestionPayload[]) {
  const map = new Map<string, { question: string; answer: string }>();
  for (const item of items ?? []) {
    const question = normalize(item.question);
    const answer = normalizeSecurityAnswer(item.answer);
    if (question && answer.length >= 3 && !map.has(question)) map.set(question, { question, answer });
  }
  return Array.from(map.values()).slice(0, 3);
}

function normalizeAnswers(items?: SecurityAnswerPayload[]) {
  const map = new Map<string, { questionId: string; answer: string }>();
  for (const item of items ?? []) {
    const questionId = (item.question_id ?? "").trim();
    const answer = normalizeSecurityAnswer(item.answer);
    if (questionId && answer.length >= 3 && !map.has(questionId)) map.set(questionId, { questionId, answer });
  }
  return Array.from(map.values()).slice(0, 3);
}

function randomToken() {
  const bytes = new Uint8Array(32);
  crypto.getRandomValues(bytes);
  return Array.from(bytes).map((b) => b.toString(16).padStart(2, "0")).join("");
}

function generateOtp() {
  const array = new Uint32Array(1);
  crypto.getRandomValues(array);
  return String(array[0] % 1_000_000).padStart(6, "0");
}

async function sha256(value: string) {
  const hash = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(hash)).map((b) => b.toString(16).padStart(2, "0")).join("");
}

function isStrongPassword(password: string) {
  return password.length >= 8 && /[a-z]/.test(password) && /[A-Z]/.test(password) && /\d/.test(password) && /[^a-zA-Z0-9]/.test(password);
}

function normalize(value?: string) { return (value ?? "").trim().toLowerCase(); }
function normalizeEmail(value?: string) { return (value ?? "").trim().toLowerCase(); }
function normalizeSecurityAnswer(value?: string) { return normalize(value).replace(/\s+/g, " "); }
function mustEnv(name: string) { const value = Deno.env.get(name)?.trim(); if (!value) throw new Error(`Missing env ${name}`); return value; }
function errorMessage(err: unknown) { return err instanceof Error ? err.message : String(err); }
function json(payload: unknown, status = 200) { return new Response(JSON.stringify(payload), { status, headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" } }); }
