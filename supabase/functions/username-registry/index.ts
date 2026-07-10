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

    if (action === "check") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, available: false, message: validation.message }, 400);

      const available = await isAvailable(admin, validation.normalized);
      return json({
        success: true,
        available,
        username: validation.display,
        message: available ? "اسم المستخدم متاح." : "اسم المستخدم محجوز، اختر اسماً آخر.",
      });
    }

    if (action === "suggest") {
      const base = sanitizeBase(body.base_name ?? body.username ?? "player");
      const candidates = buildCandidates(base);
      for (const candidate of candidates) {
        const validation = validateUsername(candidate);
        if (validation.valid && await isAvailable(admin, validation.normalized)) {
          return json({ success: true, available: true, username: validation.display, message: "تم إنشاء اسم مستخدم متاح." });
        }
      }
      return json({ success: false, available: false, message: "تعذر إنشاء اسم متاح حالياً. حاول مرة أخرى." }, 409);
    }

    if (action === "reserve") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, available: false, message: validation.message }, 400);

      const { data, error } = await admin.from("username_registry").insert({
        username: validation.display,
        normalized_username: validation.normalized,
        supabase_user_id: nullable(body.supabase_user_id),
        application_user_id: nullable(body.application_user_id),
        player_id: nullable(body.player_id),
        status: body.supabase_user_id ? "active" : "reserved",
      }).select("id, username").single();

      if (error) {
        if (error.code === "23505") {
          return json({ success: false, available: false, message: "اسم المستخدم محجوز، اختر اسماً آخر." }, 409);
        }
        console.error("username-registry:reserve_failed", error);
        return json({ success: false, available: false, message: `تعذر حجز اسم المستخدم: ${error.message}` }, 500);
      }

      return json({ success: true, available: true, username: data.username, reservation_id: data.id, message: "تم حجز اسم المستخدم بنجاح." });
    }

    if (action === "activate") {
      const validation = validateUsername(body.username ?? "");
      if (!validation.valid) return json({ success: false, message: validation.message }, 400);

      const { error } = await admin.from("username_registry").update({
        supabase_user_id: nullable(body.supabase_user_id),
        application_user_id: nullable(body.application_user_id),
        player_id: nullable(body.player_id),
        status: "active",
        updated_at: new Date().toISOString(),
      }).eq("normalized_username", validation.normalized).in("status", ["reserved", "active"]);

      if (error) {
        console.error("username-registry:activate_failed", error);
        return json({ success: false, message: `تعذر ربط اسم المستخدم بهوية اللاعب: ${error.message}` }, 500);
      }

      return json({ success: true, username: validation.display, message: "تم ربط اسم المستخدم بهوية اللاعب." });
    }

    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (error) {
    console.error("username-registry:unhandled", error);
    return json({ success: false, message: "تعذر تنفيذ عملية اسم المستخدم." }, 500);
  }
});

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
  return (cleaned || "player").slice(0, 20);
}

function buildCandidates(base: string) {
  const year = new Date().getUTCFullYear();
  const random = () => crypto.getRandomValues(new Uint32Array(1))[0] % 10000;
  return [
    base,
    `${base}_${random()}`,
    `${base}.${random()}`,
    `${base}-${year}`,
    `${base}_${String(random()).padStart(4, "0")}`,
    `${base}.pro${random()}`,
    `${base}_x${random()}`,
  ];
}

function normalize(value?: string) { return (value ?? "").trim().toLocaleLowerCase("en-US"); }
function nullable(value?: string) { const v = value?.trim(); return v ? v : null; }
function mustEnv(name: string) { const value = Deno.env.get(name)?.trim(); if (!value) throw new Error(`Missing ${name}`); return value; }
function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), { status, headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" } });
}
