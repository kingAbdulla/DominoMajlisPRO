import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });

const allowedRoles = new Set(["موظف", "مدير المنتدى", "مدير الإدارة", "مراقب"]);

function normalizeLogin(value: unknown) {
  return String(value ?? "").trim().toLowerCase();
}

function validateLogin(login: string) {
  return /^[a-z0-9._-]{3,40}$/.test(login);
}

function validatePassword(password: string) {
  return password.length >= 10
    && /[A-Za-z]/.test(password)
    && /\d/.test(password);
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (req.method !== "POST") return json({ error: "Method not allowed" }, 405);

  try {
    const url = Deno.env.get("SUPABASE_URL")!;
    const anonKey = Deno.env.get("SUPABASE_ANON_KEY")!;
    const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
    const authHeader = req.headers.get("Authorization");

    if (!authHeader) return json({ error: "غير مصرح." }, 401);

    const callerClient = createClient(url, anonKey, {
      global: { headers: { Authorization: authHeader } },
      auth: { persistSession: false },
    });
    const admin = createClient(url, serviceKey, {
      auth: { persistSession: false, autoRefreshToken: false },
    });

    const { data: callerData, error: callerError } = await callerClient.auth.getUser();
    const caller = callerData?.user;
    if (callerError || !caller) return json({ error: "جلسة الدخول غير صالحة." }, 401);

    const { data: callerProfile, error: callerProfileError } = await admin
      .from("profiles")
      .select("auth_user_id,user_id,login,role,status,disabled")
      .eq("auth_user_id", caller.id)
      .single();

    if (callerProfileError || !callerProfile) {
      return json({ error: "تعذر تحديد صلاحيات الحساب." }, 403);
    }

    const body = await req.json().catch(() => ({}));
    const action = String(body?.action ?? "");

    if (action === "change_own_password") {
      const newPassword = String(body?.newPassword ?? "");
      if (!validatePassword(newPassword)) {
        return json({ error: "كلمة المرور يجب أن تكون 10 أحرف على الأقل وتحتوي أحرفاً وأرقاماً." }, 400);
      }

      const { error: updateError } = await admin.auth.admin.updateUserById(caller.id, {
        password: newPassword,
      });
      if (updateError) throw updateError;

      const now = new Date().toISOString();
      const { error: profileError } = await admin
        .from("profiles")
        .update({
          must_change_password: false,
          password_changed_at: now,
          temporary_password_expires_at: null,
          password_reset_by_user_id: callerProfile.user_id,
        })
        .eq("auth_user_id", caller.id);
      if (profileError) throw profileError;

      return json({ ok: true });
    }

    if (callerProfile.role !== "المطور" || callerProfile.disabled || callerProfile.status !== "Active") {
      return json({ error: "هذه العملية مخصصة للمطور فقط." }, 403);
    }

    if (action === "create_user") {
      const login = normalizeLogin(body?.login);
      const fullName = String(body?.fullName ?? "").trim();
      const role = String(body?.role ?? "");
      const forumId = body?.forumId ? String(body.forumId) : null;
      const temporaryPassword = String(body?.temporaryPassword ?? "");
      const mustChangePassword = body?.mustChangePassword !== false;
      const validDays = Math.min(30, Math.max(1, Number(body?.validDays ?? 7)));

      if (!validateLogin(login)) return json({ error: "اسم المستخدم يجب أن يكون 3-40 حرفاً إنكليزياً/رقماً ويمكن استخدام . _ -" }, 400);
      if (!fullName) return json({ error: "الاسم الكامل إلزامي." }, 400);
      if (!allowedRoles.has(role)) return json({ error: "الدور غير مسموح." }, 400);
      if (role !== "مدير الإدارة" && !forumId) return json({ error: "يجب تحديد المنتدى لهذا الدور." }, 400);
      if (!validatePassword(temporaryPassword)) {
        return json({ error: "كلمة المرور المؤقتة يجب أن تكون 10 أحرف على الأقل وتحتوي أحرفاً وأرقاماً." }, 400);
      }

      const email = `${login}@forum-mis.local`;
      const { data: created, error: createError } = await admin.auth.admin.createUser({
        email,
        password: temporaryPassword,
        email_confirm: true,
      });
      if (createError) return json({ error: createError.message }, 400);
      const authUser = created.user;
      if (!authUser) return json({ error: "تعذر إنشاء حساب المصادقة." }, 500);

      const now = new Date();
      const expiresAt = mustChangePassword
        ? new Date(now.getTime() + validDays * 86400000).toISOString()
        : null;
      const userId = "USR-" + crypto.randomUUID().toUpperCase();

      const { error: profileInsertError } = await admin.from("profiles").insert({
        auth_user_id: authUser.id,
        user_id: userId,
        login,
        full_name: fullName,
        role,
        forum_id: role === "مدير الإدارة" ? null : forumId,
        status: "Active",
        disabled: false,
        must_change_password: mustChangePassword,
        temporary_password_issued_at: now.toISOString(),
        temporary_password_expires_at: expiresAt,
        password_reset_by_user_id: callerProfile.user_id,
      });

      if (profileInsertError) {
        await admin.auth.admin.deleteUser(authUser.id);
        return json({ error: profileInsertError.message }, 400);
      }

      return json({
        ok: true,
        user: { userId, login, fullName, role, forumId: role === "مدير الإدارة" ? null : forumId },
        passwordPolicy: { mustChangePassword, expiresAt },
      });
    }

    if (action === "reset_password") {
      const targetUserId = String(body?.userId ?? "");
      const temporaryPassword = String(body?.temporaryPassword ?? "");
      const mustChangePassword = body?.mustChangePassword !== false;
      const validDays = Math.min(30, Math.max(1, Number(body?.validDays ?? 7)));

      if (!targetUserId) return json({ error: "معرف المستخدم مطلوب." }, 400);
      if (!validatePassword(temporaryPassword)) {
        return json({ error: "كلمة المرور المؤقتة يجب أن تكون 10 أحرف على الأقل وتحتوي أحرفاً وأرقاماً." }, 400);
      }

      const { data: target, error: targetError } = await admin
        .from("profiles")
        .select("auth_user_id,user_id,role")
        .eq("user_id", targetUserId)
        .single();
      if (targetError || !target) return json({ error: "المستخدم غير موجود." }, 404);
      if (target.role === "المطور") return json({ error: "لا يمكن إعادة تعيين حساب المطور من هذه الشاشة." }, 403);

      const { error: resetError } = await admin.auth.admin.updateUserById(target.auth_user_id, {
        password: temporaryPassword,
      });
      if (resetError) throw resetError;

      const now = new Date();
      const expiresAt = mustChangePassword
        ? new Date(now.getTime() + validDays * 86400000).toISOString()
        : null;

      const { error: profileError } = await admin
        .from("profiles")
        .update({
          must_change_password: mustChangePassword,
          temporary_password_issued_at: now.toISOString(),
          temporary_password_expires_at: expiresAt,
          password_reset_by_user_id: callerProfile.user_id,
        })
        .eq("user_id", targetUserId);
      if (profileError) throw profileError;

      return json({ ok: true, expiresAt });
    }

    if (action === "toggle_user") {
      const targetUserId = String(body?.userId ?? "");
      const disabled = Boolean(body?.disabled);
      const { data: target, error: targetError } = await admin
        .from("profiles")
        .select("user_id,role")
        .eq("user_id", targetUserId)
        .single();

      if (targetError || !target) return json({ error: "المستخدم غير موجود." }, 404);
      if (target.role === "المطور") return json({ error: "لا يمكن تعطيل حساب المطور." }, 403);

      const { error: updateError } = await admin
        .from("profiles")
        .update({ disabled, status: disabled ? "Disabled" : "Active" })
        .eq("user_id", targetUserId);
      if (updateError) throw updateError;

      return json({ ok: true });
    }

    return json({ error: "عملية غير معروفة." }, 400);
  } catch (error) {
    console.error(error);
    return json({ error: error instanceof Error ? error.message : "حدث خطأ غير متوقع." }, 500);
  }
});
