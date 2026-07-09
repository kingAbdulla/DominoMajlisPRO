create table if not exists public.account_security_questions (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null,
    username text not null,
    email text not null,
    question text not null,
    answer_hash text not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (username, email, question)
);

create index if not exists idx_account_security_questions_username_email
    on public.account_security_questions (lower(username), lower(email));

create index if not exists idx_account_security_questions_user_id
    on public.account_security_questions (user_id);

alter table public.account_security_questions enable row level security;

grant usage on schema public to service_role;

grant select, insert, update, delete
on table public.account_security_questions
to service_role;

-- This table is intentionally service-role only.
-- Edge Functions access it with SUPABASE_SERVICE_ROLE_KEY.
-- No public RLS policies are created.
