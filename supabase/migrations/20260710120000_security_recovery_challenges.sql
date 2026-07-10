alter table public.account_security_questions
    add column if not exists application_user_id text,
    add column if not exists player_id text;

create table if not exists public.account_recovery_challenges (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null,
    username text not null,
    email text not null,
    challenge_token_hash text not null unique,
    reset_token_hash text unique,
    attempts integer not null default 0,
    max_attempts integer not null default 5,
    verified_at timestamptz,
    consumed_at timestamptz,
    expires_at timestamptz not null,
    created_at timestamptz not null default now()
);

create index if not exists idx_account_recovery_challenges_identity
    on public.account_recovery_challenges (lower(username), lower(email), created_at desc);

create index if not exists idx_account_recovery_challenges_expires
    on public.account_recovery_challenges (expires_at);

alter table public.account_recovery_challenges enable row level security;

grant usage on schema public to service_role;

grant select, insert, update, delete
on table public.account_recovery_challenges
to service_role;

grant select, insert, update, delete
on table public.account_security_questions
to service_role;

-- Both tables are service-role only. No public RLS policies are created.
