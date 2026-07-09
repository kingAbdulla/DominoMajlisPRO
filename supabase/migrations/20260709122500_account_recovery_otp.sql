create table if not exists public.account_recovery_otps (
    id uuid primary key default gen_random_uuid(),
    username text not null,
    email text not null,
    otp_hash text not null,
    purpose text not null default 'password_reset',
    attempts integer not null default 0,
    max_attempts integer not null default 5,
    expires_at timestamptz not null,
    consumed_at timestamptz,
    created_at timestamptz not null default now()
);

create index if not exists idx_account_recovery_otps_username_email
    on public.account_recovery_otps (lower(username), lower(email), created_at desc);

create index if not exists idx_account_recovery_otps_expires_at
    on public.account_recovery_otps (expires_at);

alter table public.account_recovery_otps enable row level security;

-- This table is intentionally service-role only.
-- Edge Functions access it with SUPABASE_SERVICE_ROLE_KEY.
-- No public RLS policies are created.
