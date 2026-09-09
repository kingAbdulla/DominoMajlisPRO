-- Forum MIS shared cloud database — Supabase/PostgreSQL
-- Run in Supabase SQL Editor on a new project.

create table if not exists public.profiles (
  auth_user_id uuid primary key references auth.users(id) on delete cascade,
  user_id text not null unique,
  login text not null unique,
  full_name text not null,
  role text not null check (role in ('موظف','مدير المنتدى','مدير الإدارة','المطور','مراقب')),
  forum_id text,
  status text not null default 'Active',
  disabled boolean not null default false,
  last_login_at timestamptz,
  must_change_password boolean not null default false,
  password_changed_at timestamptz,
  temporary_password_issued_at timestamptz,
  temporary_password_expires_at timestamptz,
  password_reset_by_user_id text,
  created_at timestamptz not null default now()
);

create table if not exists public.forum_directory (
  forum_id text primary key,
  code text not null unique,
  name text not null,
  active boolean not null default true,
  created_at timestamptz not null default now()
);

create table if not exists public.cloud_documents (
  collection text not null,
  row_id text not null,
  forum_id text,
  owner_user_id text,
  payload jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now(),
  updated_by uuid default auth.uid(),
  primary key (collection,row_id)
);

-- Password lifecycle fields for existing databases.
alter table public.profiles add column if not exists must_change_password boolean not null default false;
alter table public.profiles add column if not exists password_changed_at timestamptz;
alter table public.profiles add column if not exists temporary_password_issued_at timestamptz;
alter table public.profiles add column if not exists temporary_password_expires_at timestamptz;
alter table public.profiles add column if not exists password_reset_by_user_id text;

create index if not exists idx_cloud_documents_forum on public.cloud_documents(forum_id);
create index if not exists idx_cloud_documents_collection on public.cloud_documents(collection);
create index if not exists idx_cloud_documents_owner on public.cloud_documents(owner_user_id);
create index if not exists idx_profiles_forum on public.profiles(forum_id);

-- Server-authoritative synchronization metadata.
-- Client timestamps must never be authoritative for optimistic concurrency.
create or replace function public.set_cloud_document_server_metadata()
returns trigger
language plpgsql
security invoker
set search_path=public
as $
begin
  new.updated_at := now();
  new.updated_by := auth.uid();
  return new;
end;
$;

drop trigger if exists trg_cloud_documents_server_metadata on public.cloud_documents;
create trigger trg_cloud_documents_server_metadata
before insert or update on public.cloud_documents
for each row execute function public.set_cloud_document_server_metadata();


alter table public.profiles enable row level security;
alter table public.forum_directory enable row level security;
alter table public.cloud_documents enable row level security;

create or replace function public.current_app_role() returns text
language sql stable security definer set search_path=public as $$
  select role from public.profiles
  where auth_user_id=auth.uid() and disabled=false and status='Active'
  limit 1
$$;
create or replace function public.current_app_forum() returns text
language sql stable security definer set search_path=public as $$
  select forum_id from public.profiles
  where auth_user_id=auth.uid() and disabled=false and status='Active'
  limit 1
$$;
create or replace function public.current_app_user_id() returns text
language sql stable security definer set search_path=public as $$
  select user_id from public.profiles
  where auth_user_id=auth.uid() and disabled=false and status='Active'
  limit 1
$$;

revoke all on function public.current_app_role() from public;
revoke all on function public.current_app_forum() from public;
revoke all on function public.current_app_user_id() from public;
grant execute on function public.current_app_role() to authenticated;
grant execute on function public.current_app_forum() to authenticated;
grant execute on function public.current_app_user_id() to authenticated;

create or replace function public.lookup_forum(p_code text)
returns table(forum_id text,name text)
language sql stable security definer set search_path=public as $$
  select f.forum_id,f.name from public.forum_directory f
  where f.code=p_code and f.active=true limit 1
$$;
grant execute on function public.lookup_forum(text) to anon, authenticated;

drop policy if exists profiles_select on public.profiles;
drop policy if exists profiles_read_own on public.profiles;
drop policy if exists profiles_self_update on public.profiles;

-- A user can always read their own identity profile.
-- Developer/Director can read the institutional directory.
-- Forum Manager can read profiles assigned to the same ForumId.
create policy profiles_select
on public.profiles
for select
to authenticated
using (
  auth_user_id = auth.uid()
  or public.current_app_role() in ('مدير الإدارة','المطور')
  or (
    public.current_app_role() = 'مدير المنتدى'
    and forum_id = public.current_app_forum()
  )
);

-- Self-update is intentionally restricted by column privileges below.
create policy profiles_self_last_login
on public.profiles
for update
to authenticated
using (auth_user_id = auth.uid())
with check (auth_user_id = auth.uid());

drop policy if exists directory_none on public.forum_directory;
create policy directory_admin_read on public.forum_directory for select to authenticated using (
 public.current_app_role() in ('مدير الإدارة','المطور') or forum_id=public.current_app_forum()
);

-- Remove legacy/temporary policy names before installing the canonical policy set.
drop policy if exists cloud_documents_developer_select on public.cloud_documents;
drop policy if exists cloud_documents_developer_insert on public.cloud_documents;
drop policy if exists cloud_documents_developer_update on public.cloud_documents;
drop policy if exists cloud_documents_developer_delete on public.cloud_documents;
drop policy if exists cloud_documents_director_select on public.cloud_documents;
drop policy if exists cloud_documents_director_insert on public.cloud_documents;
drop policy if exists cloud_documents_director_update on public.cloud_documents;
drop policy if exists cloud_documents_director_delete on public.cloud_documents;
drop policy if exists cloud_documents_manager_select on public.cloud_documents;
drop policy if exists cloud_documents_manager_insert on public.cloud_documents;
drop policy if exists cloud_documents_manager_update on public.cloud_documents;
drop policy if exists cloud_documents_manager_delete on public.cloud_documents;
drop policy if exists cloud_documents_employee_select on public.cloud_documents;
drop policy if exists cloud_documents_employee_insert on public.cloud_documents;
drop policy if exists cloud_documents_employee_update on public.cloud_documents;
drop policy if exists cloud_documents_employee_delete on public.cloud_documents;
drop policy if exists cloud_documents_viewer_select on public.cloud_documents;

drop policy if exists docs_select on public.cloud_documents;
create policy docs_select on public.cloud_documents for select to authenticated using (
 public.current_app_role() in ('مدير الإدارة','المطور')
 or forum_id=public.current_app_forum()
 or owner_user_id=public.current_app_user_id()
 or (collection='central_entities')
);

drop policy if exists docs_insert on public.cloud_documents;
create policy docs_insert on public.cloud_documents for insert to authenticated with check (
 public.current_app_role() in ('مدير الإدارة','المطور')
 or (
   public.current_app_role()='مدير المنتدى' and forum_id=public.current_app_forum()
 )
 or (
   public.current_app_role()='موظف'
   and (
     (forum_id=public.current_app_forum() and collection in ('activities','reports','audit_events','notifications','forum_profiles','rehabilitation_records','saved_filters'))
     or (forum_id is null and collection='central_entities')
   )
 )
);

drop policy if exists docs_update on public.cloud_documents;
create policy docs_update on public.cloud_documents for update to authenticated
using (
 public.current_app_role() in ('مدير الإدارة','المطور')
 or (public.current_app_role()='مدير المنتدى' and forum_id=public.current_app_forum())
 or (public.current_app_role()='موظف' and owner_user_id=public.current_app_user_id() and collection in ('activities','reports','saved_filters','rehabilitation_records'))
)
with check (
 public.current_app_role() in ('مدير الإدارة','المطور')
 or (public.current_app_role()='مدير المنتدى' and forum_id=public.current_app_forum())
 or (public.current_app_role()='موظف' and owner_user_id=public.current_app_user_id() and collection in ('activities','reports','saved_filters','rehabilitation_records'))
);

drop policy if exists docs_delete on public.cloud_documents;
create policy docs_delete on public.cloud_documents for delete to authenticated using (
 public.current_app_role() in ('مدير الإدارة','المطور')
 or (public.current_app_role()='مدير المنتدى' and forum_id=public.current_app_forum())
);

revoke all on public.profiles from anon;
revoke all on public.cloud_documents from anon;
revoke all on public.forum_directory from anon;

-- Authenticated users may read profiles according to RLS.
-- They may only update their own last_login_at column from the browser.
revoke update on public.profiles from authenticated;
grant select on public.profiles to authenticated;
grant update(last_login_at) on public.profiles to authenticated;

grant select,insert,update,delete on public.cloud_documents to authenticated;
grant select on public.forum_directory to authenticated;

-- Edge Functions using SUPABASE_SERVICE_ROLE_KEY require explicit table privileges.
grant select,insert,update,delete on public.profiles to service_role;
grant select,insert,update,delete on public.cloud_documents to service_role;
grant select,insert,update,delete on public.forum_directory to service_role;

-- Realtime for cross-device synchronization.
do $$ begin
  alter publication supabase_realtime add table public.cloud_documents;
exception when duplicate_object then null;
end $$;

-- Initial forum directory. Extend this list later without changing frontend code.
insert into public.forum_directory(forum_id,code,name) values
 ('FRM-F001','001','منتدى الرعاية العلمية'),
 ('FRM-F002','002','منتدى الشباب'),
 ('FRM-F003','003','منتدى الرياضة'),
 ('FRM-F004','004','منتدى الثقافة والفنون')
on conflict (forum_id) do update set code=excluded.code,name=excluded.name,active=true;

-- After creating the first Auth user in Supabase Dashboard, link it:
-- Example for admin@forum-mis.local:
-- insert into public.profiles(auth_user_id,user_id,login,full_name,role,forum_id)
-- select id,'USR-U004','admin','المطور','المطور',null from auth.users where email='admin@forum-mis.local';
