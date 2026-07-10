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
  base_name?: string;
  supabase_user_id?: string;
  application_user_id?: string;
  player_id?: string;
};

serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });

  try {
    if (req.method !== "POST") return json({ success: false, message: "Method not allowed" }, 405);

    const body = await req.json() as RequestBody;
    const action = normalize(body.action);
    const admin = createClient(
      mustEnv("SUPABASE_URL"),
      mustEnv("SUPABASE_SERVICE_ROLE_KEY"),
      { auth: { autoRefreshToken: false, persistSession: false } },
    );

    if (action === "check") {
      const validation = validateUsername(body.username);
      if (!validation.valid) return json({ success: true, available: false, message: validation.message });

      const available = await isAvailable(admin, validation.usernameNormalized);
      return json({
        success: true,
        available,
        username: validation.username,
        message: available ? "اسم المستخدم متاح." : "اسم المستخدم محجوز، اختر اسمًا آخر.",
      });
    }

    if (action === "suggest") {
      const base = sanitizeBase(body.base_name ?? body.username ?? "player");
      const candidates = buildCandidates(base);
      for (const candidate of candidates) {
        const normalized = candidate.toLowerCase();
        if (await isAvailable(admin, normalized)) {
          return json({ success: true, available: true, username: candidate, message: "تم توليد اسم مستخدم متاح." });
        }
      }
      return json({ success: false, message: "تعذر توليد اسم متاح الآن. حاول مرة أخرى." }, 409);
    }

    if (action === "reserve") {
      const validation = validateUsername(body.username);
      if (!validation.valid) return json({ success: false, message: validation.message }, 400);

      const { error } = await admin.from("account_username_registry").insert({
        username: validation.username,
        username_normalized: validation.usernameNormalized,
        supabase_user_id: emptyToNull(body.supabase_user_id),
        application_user_id: emptyToNull(body.application_user_id),
        player_id: emptyToNull(body.player_id),
      });

      if (error) {
        if (error.code === "23505") {
          return json({ success: false, available: false, message: "اسم المستخدم محجوز، اختر اسمًا آخر." }, 409);
        }
        console.error("username-identity:reserve_failed", error);
        return json({ success: false, message: `تعذر حجز اسم المستخدم: ${error.code ?? "NO_CODE"}` }, 500);
      }

      return json({ success: true, available: false, username: validation.username, message: "تم حجز اسم المستخدم بنجاح." });
    }

    if (action === "bind_identity") {
      const validation = validateUsername(body.username);
      if (!validation.valid) return json({ success: false, message: validation.message }, 400);

      const { data, error } = await admin
        .from("account_username_registry")
        .update({
          supabase_user_id: emptyToNull(body.supabase_user_id),
          application_user_id: emptyToNull(body.application_user_id),
          player_id: emptyToNull(body.player_id),
          updated_at: new Date().toISOString(),
        })
        .eq("username_normalized", validation.usernameNormalized)
        .select("id")
        .maybeSingle();

      if (error) {
        console.error("username-identity:bind_failed", error);
        return json({ success: false, message: `تعذر ربط هوية اللاعب: ${error.code ?? "NO_CODE"}` }, 500);
      }
      if (!data) return json({ success: false, message: "اسم المستخدم غير محجوز." }, 404);

      return json({ success: true, message: "تم ربط Username بهوية اللاعب." });
    }

    return json({ success: false, message: "أمر غير معروف." }, 400);
  } catch (error) {
    console.error("username-identity:unhandled", error);
    return json({ success: false, message: "تعذر تنفيذ عملية اسم المستخدم." }, 500);
  }
});

async function isAvailable(admin: any, normalized: string) {
  const { data, error } = await admin
    .from("account_username_registry")
    .select("id")
    .eq("username_normalized", normalized)
    .maybeSingle();
  if (error) throw error;
  return !data;
}

function validateUsername(value?: string) {
  const username = (value ?? "").trim();
  if (username.length < 3 || username.length > 32)
    return { valid: false, username, usernameNormalized: username.toLowerCase(), message: "اسم المستخدم يجب أن يكون بين 3 و32 حرفًا." };
  if (!/^[A-Za-z0-9._-]+$/.test(username))
    return { valid: false, username, usernameNormalized: username.toLowerCase(), message: "يسمح بالحروف الإنجليزية والأرقام والرموز . _ - فقط." };
  return { valid: true, username, usernameNormalized: username.toLowerCase(), message: "" };
}

function sanitizeBase(value: string) {
  let result = value.trim().replace(/\s+/g, "").replace(/[^A-Za-z0-9._-]/g, "");
  if (result.length < 3) result = "Player";
  return result.slice(0, 20);
}

function buildCandidates(base: string) {
  const year = new Date().getUTCFullYear();
  const random = () => crypto.getRandomValues(new Uint32Array(1))[0] % 10000;
  return [
    `${base}${random()}`,
    `${base}_${random()}`,
    `${base}.${random()}`,
    `${base}-${year}`,
    `${base}_x${random() % 100}`,
    `${base}.${year % 100}${random() % 100}`,
  ].map((item) => item.slice(0, 32));
}

function normalize(value?: string) {
  return (value ?? "").trim().toLowerCase();
}

function emptyToNull(value?: string) {
  const cleaned = (value ?? "").trim();
  return cleaned.length === 0 ? null : cleaned;
}

function mustEnv(name: string) {
  const value = Deno.env.get(name)?.trim();
  if (!value) throw new Error(`Missing env ${name}`);
  return value;
}

function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}
