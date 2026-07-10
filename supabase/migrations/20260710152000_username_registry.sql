create table if not exists public.username_registry (
    id uuid primary key default gen_random_uuid(),
    username text not null,
    normalized_username text not null,
    supabase_user_id uuid,
    application_user_id text,
    player_id text,
    status text not null default 'reserved',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint username_registry_username_length check (char_length(username) between 3 and 32),
    constraint username_registry_status check (status in ('reserved','active','released'))
);

create unique index if not exists ux_username_registry_normalized_active
    on public.username_registry (normalized_username)
    where status in ('reserved','active');

create unique index if not exists ux_username_registry_supabase_user_active
    on public.username_registry (supabase_user_id)
    where supabase_user_id is not null and status = 'active';

create unique index if not exists ux_username_registry_player_active
    on public.username_registry (player_id)
    where player_id is not null and status = 'active';

alter table public.username_registry enable row level security;

grant usage on schema public to service_role;
grant select, insert, update, delete on public.username_registry to service_role;
