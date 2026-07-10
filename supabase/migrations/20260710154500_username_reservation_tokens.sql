alter table public.username_registry
    add column if not exists reservation_token text,
    add column if not exists reserved_until timestamptz;

create index if not exists idx_username_registry_reserved_until
    on public.username_registry (reserved_until)
    where status = 'reserved';

update public.username_registry
set reserved_until = coalesce(reserved_until, created_at + interval '15 minutes')
where status = 'reserved';
