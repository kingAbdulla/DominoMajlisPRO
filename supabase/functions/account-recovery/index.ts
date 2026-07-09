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
      return json({ success: false, message: "Method not allowed" }, 405);
    }

    const body = await req.json() as RequestBody;
    const action = normalize(body.action);
    const username = normalize(body.username);
    const email = normalizeEmail(body.email);

    if (!username || !email) {
      return json({ success: false, message: "اسم المستخدم والبريد الإلكتروني مطلوبان." }, 400);
    }

    const supabaseUrl = mustEnv("SUPABASE_URL");
    const serviceRoleKey = mustEnv("SUPABASE_SERVICE_ROLE_KEY");
    const admin = createClient(supabaseUrl, serviceRoleKey, {
      auth: { autoRefreshToken: false, persistSession: false },
    });

    const user = await findUserByUsernameAndEmail(admin, username, email);
    if (!user) {
      // Do not reveal whether an account exists.
      return json({ success: true, message: "إذا كانت البيانات صحيحة فسيتم إرسال رمز التحقق." });
    }

    if (action === "request_email_otp") {
      const otp = generateOtp();
      const otpHash = await sha256(`${username}:${email}:${otp}:${mustEnv("ACCOUNT_RECOVERY_OTP_PEPPER")}`);
      const expiresAt = new Date(Date.now() + 10 * 60 * 1000).toISOString();

      await admin
        .from("account_recovery_otps")
        .insert({
          username,
          email,
          otp_hash: otpHash,
          purpose: "password_reset",
          expires_at: expiresAt,
        });

      await sendOtpEmail(email, username, otp);

      return json({ success: true, message: "تم إرسال رمز التحقق إلى البريد الإلكتروني." });
    }

    if (action === "verify_email_otp_reset") {
      const otp = normalize(body.otp);
      const newPassword = body.new_password ?? "";

      if (!isStrongPassword(newPassword)) {
        return json({ success: false, message: "كلمة المرور الجديدة لا تطابق شروط القوة." }, 400);
      }

      if (!otp || otp.length !== 6) {
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
        return json({ success: false, message: "تعذر التحقق من الرمز." }, 500);
      }

      const row = rows?.[0];
      if (!row) {
        return json({ success: false, message: "رمز التحقق غير صحيح." }, 400);
      }

      if (row.consumed_at) {
        return json({ success: false, message: "تم استخدام هذا الرمز مسبقاً." }, 400);
      }

      if (new Date(row.expires_at).getTime() < Date.now()) {
        return json({ success: false, message: "انتهت صلاحية رمز التحقق." }, 400);
      }

      if ((row.attempts ?? 0) >= (row.max_attempts ?? 5)) {
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
        return json({ success: false, message: "تعذر تحديث كلمة المرور." }, 500);
      }

      await admin
        .from("account_recovery_otps")
        .update({ consumed_at: new Date().toISOString() })
        .eq("id", row.id);

      return json({ success: true, message: "تم تحديث كلمة المرور بنجاح." });
    }

    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (err) {
    console.error(err);
    return json({ success: false, message: "تعذر تنفيذ عملية الاسترداد." }, 500);
  }
});

async function findUserByUsernameAndEmail(admin: any, username: string, email: string) {
  let page = 1;
  const perPage = 200;

  while (page <= 20) {
    const { data, error } = await admin.auth.admin.listUsers({ page, perPage });
    if (error) throw error;

    const user = data?.users?.find((item: any) => {
      const metadataUsername = normalize(item?.user_metadata?.username);
      const itemEmail = normalizeEmail(item?.email);
      return metadataUsername === username && itemEmail === email;
    });

    if (user) return user;
    if (!data?.users || data.users.length < perPage) return null;
    page++;
  }

  return null;
}

async function sendOtpEmail(email: string, username: string, otp: string) {
  const resendApiKey = Deno.env.get("RESEND_API_KEY")?.trim() ?? "";
  const fromEmail = Deno.env.get("ACCOUNT_RECOVERY_FROM_EMAIL")?.trim() ?? "";

  if (!resendApiKey || !fromEmail) {
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
    console.error("Resend failed", text);
    throw new Error("Failed to send OTP email");
  }
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
