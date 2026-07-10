create table if not exists public.account_identity_registry (
    id uuid primary key default gen_random_uuid(),
    supabase_user_id uuid not null unique,
    username text not null,
    normalized_username text not null unique,
    email text not null,
    normalized_email text not null unique,
    application_user_id text,
    player_id text,
    display_name text not null default '',
    email_verified boolean not null default false,
    email_verified_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_account_identity_registry_username
    on public.account_identity_registry (normalized_username);

create unique index if not exists ux_account_identity_registry_email
    on public.account_identity_registry (normalized_email);

create table if not exists public.account_email_verification_otps (
    id uuid primary key default gen_random_uuid(),
    supabase_user_id uuid not null,
    normalized_email text not null,
    otp_hash text not null,
    expires_at timestamptz not null,
    attempts integer not null default 0,
    max_attempts integer not null default 5,
    consumed_at timestamptz,
    created_at timestamptz not null default now()
);

create index if not exists idx_account_email_verification_otps_user
    on public.account_email_verification_otps (supabase_user_id, created_at desc);

alter table public.account_identity_registry enable row level security;
alter table public.account_email_verification_otps enable row level security;

grant usage on schema public to service_role;
grant select, insert, update, delete on table public.account_identity_registry to service_role;
grant select, insert, update, delete on table public.account_email_verification_otps to service_role;

-- Both tables are intentionally service-role only. All public access goes through Edge Functions.
