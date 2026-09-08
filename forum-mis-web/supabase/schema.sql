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

create index if not exists idx_cloud_documents_forum on public.cloud_documents(forum_id);
create index if not exists idx_cloud_documents_collection on public.cloud_documents(collection);
create index if not exists idx_cloud_documents_owner on public.cloud_documents(owner_user_id);
create index if not exists idx_profiles_forum on public.profiles(forum_id);

alter table public.profiles enable row level security;
alter table public.forum_directory enable row level security;
alter table public.cloud_documents enable row level security;

create or replace function public.current_app_role() returns text
language sql stable security definer set search_path=public as $$
  select role from public.profiles where auth_user_id=auth.uid()
$$;
create or replace function public.current_app_forum() returns text
language sql stable security definer set search_path=public as $$
  select forum_id from public.profiles where auth_user_id=auth.uid()
$$;
create or replace function public.current_app_user_id() returns text
language sql stable security definer set search_path=public as $$
  select user_id from public.profiles where auth_user_id=auth.uid()
$$;

create or replace function public.lookup_forum(p_code text)
returns table(forum_id text,name text)
language sql stable security definer set search_path=public as $$
  select f.forum_id,f.name from public.forum_directory f
  where f.code=p_code and f.active=true limit 1
$$;
grant execute on function public.lookup_forum(text) to anon, authenticated;

drop policy if exists profiles_select on public.profiles;
create policy profiles_select on public.profiles for select to authenticated using (
 auth_user_id=auth.uid()
 or forum_id=public.current_app_forum()
 or role in ('مدير الإدارة','المطور')
 or public.current_app_role() in ('مدير الإدارة','المطور')
);
drop policy if exists profiles_self_update on public.profiles;
create policy profiles_self_update on public.profiles for update to authenticated
using (auth_user_id=auth.uid()) with check (auth_user_id=auth.uid());

drop policy if exists directory_none on public.forum_directory;
create policy directory_admin_read on public.forum_directory for select to authenticated using (
 public.current_app_role() in ('مدير الإدارة','المطور') or forum_id=public.current_app_forum()
);

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
grant select,update on public.profiles to authenticated;
grant select,insert,update,delete on public.cloud_documents to authenticated;
grant select on public.forum_directory to authenticated;

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
