import { serve } from "https://deno.land/std@0.224.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.4";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

type Body = {
  action?: string;
  username?: string;
  base_name?: string;
  reservation_token?: string;
  supabase_user_id?: string;
  application_user_id?: string;
  player_id?: string;
};

serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });

  try {
    if (req.method !== "POST") return json({ success: false, message: "Method not allowed" }, 405);

    const body = await req.json() as Body;
    const action = normalize(body.action);
    const admin = createClient(mustEnv("SUPABASE_URL"), mustEnv("SUPABASE_SERVICE_ROLE_KEY"), {
      auth: { persistSession: false, autoRefreshToken: false },
    });

    await releaseExpiredReservations(admin);

    if (action === "check") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, available: false, message: validation.message }, 400);

      const available = await isAvailable(admin, validation.normalized);
      return json({
        success: true,
        available,
        username: validation.display,
        message: available ? "✓ اسم المستخدم متاح." : "✕ اسم المستخدم مستخدم بالفعل.",
      });
    }

    if (action === "suggest") {
      const base = sanitizeBase(body.base_name ?? body.username ?? "player");
      const candidates = buildCandidates(base);
      for (const candidate of candidates) {
        const validation = validateUsername(candidate);
        if (validation.valid && await isAvailable(admin, validation.normalized)) {
          return json({
            success: true,
            available: true,
            username: validation.display,
            message: "✓ اسم المستخدم متاح.",
          });
        }
      }
      return json({ success: false, available: false, message: "تعذر إنشاء اسم متاح حالياً. حاول مرة أخرى." }, 409);
    }

    if (action === "reserve") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, available: false, message: validation.message }, 400);

      const reservationToken = crypto.randomUUID().replaceAll("-", "") + crypto.randomUUID().replaceAll("-", "");
      const reservedUntil = new Date(Date.now() + 15 * 60 * 1000).toISOString();

      const { data, error } = await admin.from("username_registry").insert({
        username: validation.display,
        normalized_username: validation.normalized,
        reservation_token: reservationToken,
        reserved_until: reservedUntil,
        supabase_user_id: nullable(body.supabase_user_id),
        application_user_id: nullable(body.application_user_id),
        player_id: nullable(body.player_id),
        status: body.supabase_user_id ? "active" : "reserved",
      }).select("id, username").single();

      if (error) {
        if (error.code === "23505") {
          return json({ success: false, available: false, message: "اسم المستخدم حُجز للتو بواسطة مستخدم آخر. اختر اسماً آخر." }, 409);
        }
        console.error("username-registry:reserve_failed", error);
        return json({ success: false, available: false, message: "تعذر إكمال إنشاء الحساب حالياً." }, 500);
      }

      return json({
        success: true,
        available: true,
        username: data.username,
        reservation_id: data.id,
        reservation_token: reservationToken,
        reserved_until: reservedUntil,
        message: "تم تأمين الاسم أثناء تنفيذ إنشاء الحساب.",
      });
    }

    if (action === "release") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, message: validation.message }, 400);

      const reservationToken = (body.reservation_token ?? "").trim();
      if (!reservationToken) return json({ success: false, message: "رمز الحجز مفقود." }, 400);

      const { error } = await admin.from("username_registry")
        .update({ status: "released", reserved_until: null, updated_at: new Date().toISOString() })
        .eq("normalized_username", validation.normalized)
        .eq("reservation_token", reservationToken)
        .eq("status", "reserved");

      if (error) {
        console.error("username-registry:release_failed", error);
        return json({ success: false, message: "تعذر تحرير اسم المستخدم." }, 500);
      }

      return json({ success: true, available: true, username: validation.display, message: "تم تحرير اسم المستخدم." });
    }

    if (action === "activate") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, message: validation.message }, 400);

      const reservationToken = (body.reservation_token ?? "").trim();
      if (!reservationToken) return json({ success: false, message: "رمز حجز اسم المستخدم مفقود." }, 400);

      const { data: reservation, error: lookupError } = await admin.from("username_registry")
        .select("id, status, reserved_until, reservation_token")
        .eq("normalized_username", validation.normalized)
        .in("status", ["reserved", "active"])
        .limit(1)
        .maybeSingle();

      if (lookupError) throw lookupError;
      if (!reservation || reservation.reservation_token !== reservationToken) {
        return json({ success: false, message: "تعذر التحقق من اسم المستخدم." }, 409);
      }
      if (reservation.status === "reserved" && new Date(reservation.reserved_until).getTime() < Date.now()) {
        return json({ success: false, message: "انتهت مهلة إنشاء الحساب. أعد المحاولة." }, 409);
      }

      const { error } = await admin.from("username_registry").update({
        supabase_user_id: nullable(body.supabase_user_id),
        application_user_id: nullable(body.application_user_id),
        player_id: nullable(body.player_id),
        status: "active",
        reserved_until: null,
        updated_at: new Date().toISOString(),
      }).eq("id", reservation.id);

      if (error) {
        console.error("username-registry:activate_failed", error);
        return json({ success: false, message: "تعذر ربط اسم المستخدم بالحساب." }, 500);
      }

      return json({ success: true, username: validation.display, message: "تم ربط اسم المستخدم بالحساب." });
    }

    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (error) {
    console.error("username-registry:unhandled", error);
    return json({ success: false, message: "تعذر تنفيذ عملية اسم المستخدم." }, 500);
  }
});

async function releaseExpiredReservations(admin: any) {
  const { error } = await admin.from("username_registry")
    .update({ status: "released", updated_at: new Date().toISOString() })
    .eq("status", "reserved")
    .lt("reserved_until", new Date().toISOString());
  if (error) console.warn("username-registry:expired_release_failed", error);
}

async function isAvailable(admin: any, normalized: string) {
  const { data, error } = await admin.from("username_registry")
    .select("id")
    .eq("normalized_username", normalized)
    .in("status", ["reserved", "active"])
    .limit(1);
  if (error) throw error;
  return !data || data.length === 0;
}

function validateUsername(value: string) {
  const display = value.trim();
  const normalized = normalize(display);
  if (display.length < 3 || display.length > 32)
    return { valid: false, display, normalized, message: "اسم المستخدم يجب أن يكون بين 3 و32 حرفاً." };
  if (!/^[\p{L}\p{N}._-]+$/u.test(display))
    return { valid: false, display, normalized, message: "يسمح بالحروف والأرقام والرموز . _ - فقط." };
  if (display.includes("@"))
    return { valid: false, display, normalized, message: "لا يمكن استخدام @ داخل اسم المستخدم." };
  return { valid: true, display, normalized, message: "" };
}

function sanitizeBase(value: string) {
  const cleaned = value.trim().replace(/\s+/g, "").replace(/[^\p{L}\p{N}._-]/gu, "");
  return (cleaned || "player").slice(0, 18);
}

function buildCandidates(base: string) {
  const year = new Date().getUTCFullYear();
  const random2 = () => 10 + crypto.getRandomValues(new Uint32Array(1))[0] % 90;
  const random3 = () => 100 + crypto.getRandomValues(new Uint32Array(1))[0] % 900;
  const random4 = () => 1000 + crypto.getRandomValues(new Uint32Array(1))[0] % 9000;

  return [
    base,
    `${base}${random2()}`,
    `${base}_${random2()}`,
    `${base}.${random2()}`,
    `${base}${random3()}`,
    `${base}_${year}`,
    `${base}.pro`,
    `${base}_x${random2()}`,
    `${base}-${random3()}`,
    `${base}_${random4()}`,
  ];
}

function normalize(value?: string) { return (value ?? "").trim().toLocaleLowerCase("en-US"); }
function nullable(value?: string) { const v = value?.trim(); return v ? v : null; }
function mustEnv(name: string) { const value = Deno.env.get(name)?.trim(); if (!value) throw new Error(`Missing ${name}`); return value; }
function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), { status, headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" } });
}
