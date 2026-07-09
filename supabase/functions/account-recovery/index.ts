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
  otp?: string;
  new_password?: string;
};

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    if (req.method !== "POST") {
      console.warn("account-recovery:invalid_method", req.method);
      return json({ success: false, message: "Method not allowed" }, 405);
    }

    const body = await req.json() as RequestBody;
    const action = normalize(body.action);
    const username = normalize(body.username);
    const email = normalizeEmail(body.email);

    console.log("account-recovery:request", {
      action,
      username,
      emailDomain: email.includes("@") ? email.split("@")[1] : "invalid",
    });

    if (!username || !email) {
      console.warn("account-recovery:missing_identity_fields");
      return json({ success: false, message: "اسم المستخدم والبريد الإلكتروني مطلوبان." }, 400);
    }

    const supabaseUrl = mustEnv("SUPABASE_URL");
    const serviceRoleKey = mustEnv("SUPABASE_SERVICE_ROLE_KEY");
    const admin = createClient(supabaseUrl, serviceRoleKey, {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    const user = await findUserByUsernameAndEmail(admin, username, email);
    if (!user) {
      console.warn("account-recovery:user_not_found_or_metadata_mismatch", {
        username,
        emailDomain: email.includes("@") ? email.split("@")[1] : "invalid",
      });
      return json({ success: true, message: "إذا كانت البيانات صحيحة فسيتم إرسال رمز التحقق." });
    }

    console.log("account-recovery:user_verified", {
      username,
      userId: user.id,
      emailConfirmed: Boolean(user.email_confirmed_at),
    });

    if (action === "request_email_otp") {
      const otp = generateOtp();
      const otpHash = await sha256(`${username}:${email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const expiresAt = new Date(Date.now() + 10 * 60 * 1000).toISOString();

      const { error: insertError } = await admin
        .from("account_recovery_otps")
        .insert({
          username,
          email,
          otp_hash: otpHash,
          purpose: "password_reset",
          expires_at: expiresAt,
        });

      if (insertError) {
        console.error("account-recovery:otp_insert_failed", insertError);
        return json(
          {
            success: false,
            message: `تعذر إنشاء رمز الاسترداد: ${insertError.code ?? "NO_CODE"} - ${insertError.message ?? "NO_MESSAGE"}`,
          },
          500,
        );
      }

      await sendOtpEmail(email, username, otp);
      console.log("account-recovery:otp_email_sent", { username });

      return json({ success: true, message: "تم إرسال رمز التحقق إلى البريد الإلكتروني." });
    }

    if (action === "verify_email_otp_reset") {
      const otp = normalize(body.otp);
      const newPassword = body.new_password ?? "";

      if (!isStrongPassword(newPassword)) {
        console.warn("account-recovery:weak_new_password", { username });
        return json({ success: false, message: "كلمة المرور الجديدة لا تطابق شروط القوة." }, 400);
      }

      if (!otp || otp.length !== 6) {
        console.warn("account-recovery:invalid_otp_format", { username });
        return json({ success: false, message: "رمز التحقق غير صالح." }, 400);
      }

      const otpHash = await sha256(`${username}:${email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const { data: rows, error } = await admin
        .from("account_recovery_otps")
        .select("id, attempts, max_attempts, expires_at, consumed_at")
        .eq("username", username)
        .eq("email", email)
        .eq("otp_hash", otpHash)
        .eq("purpose", "password_reset")
        .order("created_at", { ascending: false })
        .limit(1);

      if (error) {
        console.error("account-recovery:otp_lookup_failed", error);
        return json({ success: false, message: "تعذر التحقق من الرمز." }, 500);
      }

      const row = rows?.[0];
      if (!row) {
        console.warn("account-recovery:otp_not_found", { username });
        return json({ success: false, message: "رمز التحقق غير صحيح." }, 400);
      }

      if (row.consumed_at) {
        console.warn("account-recovery:otp_already_consumed", { username });
        return json({ success: false, message: "تم استخدام هذا الرمز مسبقاً." }, 400);
      }

      if (new Date(row.expires_at).getTime() < Date.now()) {
        console.warn("account-recovery:otp_expired", { username });
        return json({ success: false, message: "انتهت صلاحية رمز التحقق." }, 400);
      }

      if ((row.attempts ?? 0) >= (row.max_attempts ?? 5)) {
        console.warn("account-recovery:otp_attempts_exceeded", { username });
        return json({ success: false, message: "تم تجاوز عدد المحاولات المسموح." }, 429);
      }

      await admin
        .from("account_recovery_otps")
        .update({ attempts: (row.attempts ?? 0) + 1 })
        .eq("id", row.id);

      const { error: updateError } = await admin.auth.admin.updateUserById(user.id, {
        password: newPassword,
      });

      if (updateError) {
        console.error("account-recovery:password_update_failed", updateError);
        return json({ success: false, message: "تعذر تحديث كلمة المرور." }, 500);
      }

      await admin
        .from("account_recovery_otps")
        .update({ consumed_at: new Date().toISOString() })
        .eq("id", row.id);

      console.log("account-recovery:password_reset_success", { username, userId: user.id });
      return json({ success: true, message: "تم تحديث كلمة المرور بنجاح." });
    }

    console.warn("account-recovery:unknown_action", { action });
    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (err) {
    console.error("account-recovery:unhandled_error", err);
    return json({ success: false, message: "تعذر تنفيذ عملية الاسترداد." }, 500);
  }
});

async function findUserByUsernameAndEmail(admin: any, username: string, email: string) {
  let page = 1;
  const perPage = 200;

  while (page <= 20) {
    const { data, error } = await admin.auth.admin.listUsers({ page, perPage });
    if (error) throw error;

    console.log("account-recovery:scan_users_page", {
      page,
      count: data?.users?.length ?? 0,
    });

    const user = data?.users?.find((item: any) => {
      const metadataUsername = normalize(item?.user_metadata?.username);
      const metadataNickname = normalize(item?.user_metadata?.nickname);
      const metadataDisplayName = normalize(item?.user_metadata?.display_name);
      const itemEmail = normalizeEmail(item?.email);
      const identityMatches =
        metadataUsername === username ||
        metadataNickname === username ||
        metadataDisplayName === username;

      return identityMatches && itemEmail === email;
    });

    if (user) {
      console.log("account-recovery:identity_match", {
        matchedUsername: Boolean(normalize(user?.user_metadata?.username) === username),
        matchedNickname: Boolean(normalize(user?.user_metadata?.nickname) === username),
        matchedDisplayName: Boolean(normalize(user?.user_metadata?.display_name) === username),
      });
      return user;
    }

    if (!data?.users || data.users.length < perPage) return null;
    page++;
  }

  return null;
}

async function sendOtpEmail(email: string, username: string, otp: string) {
  const resendApiKey = Deno.env.get("RESEND_API_KEY")?.trim() ?? "";
  const fromEmail = Deno.env.get("ACCOUNT_RECOVERY_FROM_EMAIL")?.trim() ?? "";

  console.log("account-recovery:email_config", {
    hasResendApiKey: Boolean(resendApiKey),
    fromEmail,
  });

  if (!resendApiKey || !fromEmail) {
    console.warn("account-recovery:email_secrets_missing");
    console.log(`Account recovery OTP for ${username}/${email}: ${otp}`);
    return;
  }

  const response = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${resendApiKey}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      from: fromEmail,
      to: email,
      subject: "Domino Majlis PRO - رمز استعادة الحساب",
      html: `
        <div dir="rtl" style="font-family:Arial,sans-serif;background:#050505;color:#fff;padding:24px;border-radius:16px">
          <h2 style="color:#D4AF37">Domino Majlis PRO</h2>
          <p>رمز استعادة الحساب الخاص بك هو:</p>
          <div style="font-size:28px;font-weight:bold;letter-spacing:6px;color:#D4AF37">${otp}</div>
          <p>ينتهي الرمز خلال 10 دقائق. لا تشارك هذا الرمز مع أي شخص.</p>
        </div>
      `,
    }),
  });

  if (!response.ok) {
    const text = await response.text();
    console.error("account-recovery:resend_failed", {
      status: response.status,
      body: text,
    });
    throw new Error("Failed to send OTP email");
  }

  const text = await response.text();
  console.log("account-recovery:resend_success", text);
}

function generateOtp() {
  const array = new Uint32Array(1);
  crypto.getRandomValues(array);
  return String(array[0] % 1_000_000).padStart(6, "0");
}

async function sha256(value: string) {
  const data = new TextEncoder().encode(value);
  const hash = await crypto.subtle.digest("SHA-256", data);
  return Array.from(new Uint8Array(hash)).map((b) => b.toString(16).padStart(2, "0")).join("");
}

function isStrongPassword(password: string) {
  return password.length >= 8 &&
    /[a-z]/.test(password) &&
    /[A-Z]/.test(password) &&
    /\d/.test(password) &&
    /[^a-zA-Z0-9]/.test(password);
}

function normalize(value?: string) {
  return (value ?? "").trim().toLowerCase();
}

function normalizeEmail(value?: string) {
  return (value ?? "").trim().toLowerCase();
}

function mustEnv(name: string) {
  const value = Deno.env.get(name)?.trim();
  if (!value) throw new Error(`Missing env ${name}`);
  return value;
}

function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json; charset=utf-8",
    },
  });
}
