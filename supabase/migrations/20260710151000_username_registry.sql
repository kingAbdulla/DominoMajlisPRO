create table if not exists public.account_username_registry (
    id uuid primary key default gen_random_uuid(),
    username text not null,
    username_normalized text not null,
    supabase_user_id uuid,
    application_user_id text,
    player_id text,
    reserved_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint account_username_registry_username_length check (char_length(username) between 3 and 32),
    constraint account_username_registry_username_format check (username ~ '^[A-Za-z0-9._-]+$')
);

create unique index if not exists ux_account_username_registry_normalized
    on public.account_username_registry (username_normalized);

create unique index if not exists ux_account_username_registry_supabase_user
    on public.account_username_registry (supabase_user_id)
    where supabase_user_id is not null;

create unique index if not exists ux_account_username_registry_player
    on public.account_username_registry (player_id)
    where player_id is not null and player_id <> '';

alter table public.account_username_registry enable row level security;

grant usage on schema public to service_role;
grant select, insert, update, delete on table public.account_username_registry to service_role;

comment on table public.account_username_registry is
'Global permanent username registry for Domino Majlis PRO. Access is service-role only.';
