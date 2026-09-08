(function(){
  const cfg=window.FORUM_MIS_CLOUD_CONFIG||{};
  const MAP={
    v29_forums:{collection:"forums",id:["forumId","id"],forum:["forumId","id"],owner:[]},
    v29_acts:{collection:"activities",id:["activityId","id"],forum:["forumId","forum"],owner:["dataEntryUserId","createdBy"]},
    v29_reports:{collection:"reports",id:["reportId","id"],forum:["forumId","forum"],owner:["writerUserId","createdBy"]},
    v29_audit:{collection:"audit_events",id:["auditEventId","id","ref"],forum:["forumId"],owner:["actorUserId","user"]},
    v29_notifs:{collection:"notifications",id:["notificationId","id"],forum:["forumId"],owner:["recipientUserId","userId","recipient"]},
    v29_forum_profiles:{collection:"forum_profiles",id:["forumProfileId","id"],forum:["forumId","forum"],owner:["updatedByUserId","createdByUserId"]},
    v29_rehabilitation_records:{collection:"rehabilitation_records",id:["rehabilitationRecordId","id"],forum:["forumId","forum"],owner:["createdByUserId"]},
    v29_central_entities:{collection:"central_entities",id:["entityId","id"],forum:[],owner:["createdByUserId"]},
    v29_saved_filters:{collection:"saved_filters",id:["savedFilterId","id"],forum:["forumId"],owner:["ownerUserId"]}
  };
  const REVERSE=Object.fromEntries(Object.entries(MAP).map(([k,v])=>[v.collection,k]));
  let client=null,profile=null,profiles=[],channel=null,pullTimer=null,writeTimers=new Map(),hydrating=false;
  const pick=(o,keys)=>{for(const k of keys||[])if(o&&o[k])return o[k];return null};
  const slug=v=>String(v||"").trim().toLowerCase().replace(/[^a-z0-9._-]/g,"-");
  const emailForLogin=login=>String(login||"").includes("@")?String(login).trim():slug(login)+"@"+(cfg.loginDomain||"forum-mis.local");
  const configured=()=>!!(cfg.enabled&&cfg.url&&cfg.publishableKey&&window.supabase?.createClient);
  function emit(status,text){window.dispatchEvent(new CustomEvent("forum-mis-cloud-status",{detail:{status,text}}))}
  async function init(){
    if(!configured()){emit("offline","محلي");return false}
    client=window.supabase.createClient(cfg.url,cfg.publishableKey,{auth:{persistSession:true,autoRefreshToken:true,detectSessionInUrl:true}});
    const {data}=await client.auth.getSession();
    if(data?.session){await loadProfile();await subscribe()}
    emit(data?.session?"online":"ready",data?.session?"سحابي متصل":"السحابة جاهزة");
    return true
  }
  async function lookupForum(code){
    if(!configured())return null;if(!client)await init();
    const {data,error}=await client.rpc("lookup_forum",{p_code:String(code||"").trim()});
    if(error)throw error;const r=Array.isArray(data)?data[0]:data;
    return r?{forumId:r.forum_id,id:r.forum_id,name:r.name,code:String(code),active:true}:null
  }
  async function loadProfile(){
    if(!client)return null;
    const {data:{user}}=await client.auth.getUser();
    if(!user)return null;

    // Phase 1: always load the current user's own profile first.
    const {data,error}=await client
      .from("profiles")
      .select("auth_user_id,user_id,login,full_name,role,forum_id,status,disabled,last_login_at,must_change_password,password_changed_at,temporary_password_issued_at,temporary_password_expires_at,password_reset_by_user_id")
      .eq("auth_user_id",user.id)
      .maybeSingle();

    if(error)throw new Error("تعذر تحميل ملف صلاحيات الحساب: "+error.message);
    if(!data)throw new Error("لا يوجد ملف صلاحيات مرتبط بهذا الحساب.");

    profile=data;

    // Phase 2: profile directory visibility is role-scoped.
    // Do not fail login if the user cannot read other profiles.
    profiles=[{
      id:data.user_id,
      userId:data.user_id,
      login:data.login,
      name:data.full_name,
      role:data.role,
      forum:data.forum_id,
      forumId:data.forum_id,
      status:data.status,
      disabled:!!data.disabled,
      lastLoginAt:data.last_login_at,
      mustChangePassword:!!data.must_change_password,
      passwordChangedAt:data.password_changed_at,
      temporaryPasswordIssuedAt:data.temporary_password_issued_at,
      temporaryPasswordExpiresAt:data.temporary_password_expires_at,
      passwordResetByUserId:data.password_reset_by_user_id,
      authUserId:data.auth_user_id
    }];

    if(["المطور","مدير الإدارة","مدير المنتدى"].includes(data.role)){
      const q=await client
        .from("profiles")
        .select("auth_user_id,user_id,login,full_name,role,forum_id,status,disabled,last_login_at,must_change_password,password_changed_at,temporary_password_issued_at,temporary_password_expires_at,password_reset_by_user_id");
      if(!q.error){
        profiles=(q.data||[]).map(p=>({
          id:p.user_id,
          userId:p.user_id,
          login:p.login,
          name:p.full_name,
          role:p.role,
          forum:p.forum_id,
          forumId:p.forum_id,
          status:p.status,
          disabled:!!p.disabled,
          lastLoginAt:p.last_login_at,
          mustChangePassword:!!p.must_change_password,
          passwordChangedAt:p.password_changed_at,
          temporaryPasswordIssuedAt:p.temporary_password_issued_at,
          temporaryPasswordExpiresAt:p.temporary_password_expires_at,
          passwordResetByUserId:p.password_reset_by_user_id,
          authUserId:p.auth_user_id
        }));
      } else {
        console.warn("Profile directory is restricted by RLS:",q.error.message);
      }
    }

    return profile
  }
  async function signIn(login,password){
    if(!configured())throw new Error("Cloud is not configured");
    if(!client)await init();
    const email=emailForLogin(login);
    const {data,error}=await client.auth.signInWithPassword({email,password});
    if(error)throw error;
    await loadProfile();
    if(profile.disabled||profile.status==="Disabled"){await client.auth.signOut();throw new Error("هذا الحساب معطل")}
    const loginStamp=await client.from("profiles").update({last_login_at:new Date().toISOString()}).eq("auth_user_id",data.user.id);
    if(loginStamp.error)console.warn("Last login timestamp not persisted yet:",loginStamp.error.message);
    await subscribe();
    emit("online","سحابي متصل");
    return {profile,profiles};
  }
  async function adminUserAction(action,payload={}){
    if(!client||!profile)throw new Error("يجب تسجيل الدخول أولاً");
    if(profile.role!=="المطور")throw new Error("هذه العملية مخصصة للمطور فقط");
    const {data,error}=await client.functions.invoke("admin-users",{body:{action,...payload}});
    if(error)throw error;
    if(data?.error)throw new Error(data.error);
    await loadProfile();
    return data;
  }
  async function changeOwnPassword(newPassword){
    if(!client||!profile)throw new Error("يجب تسجيل الدخول أولاً");
    const {data,error}=await client.functions.invoke("admin-users",{body:{action:"change_own_password",newPassword}});
    if(error)throw error;
    if(data?.error)throw new Error(data.error);
    await loadProfile();
    return data;
  }
  async function signOut(){if(client)await client.auth.signOut();profile=null;profiles=[];if(channel){await client.removeChannel(channel);channel=null}emit(configured()?"ready":"offline",configured()?"السحابة جاهزة":"محلي")}
  function legacyCurrent(){
    if(!profile)return null;return{id:profile.user_id,userId:profile.user_id,login:profile.login,name:profile.full_name,role:profile.role,forum:profile.forum_id,forumId:profile.forum_id,status:profile.status,disabled:!!profile.disabled,authUserId:profile.auth_user_id}
  }
  function rowsToSnapshot(rows){
    const snap={};Object.keys(MAP).forEach(k=>snap[k]=[]);
    for(const r of rows||[]){const k=REVERSE[r.collection];if(k)snap[k].push(r.payload)}
    return snap
  }
  async function hydrate(){
    if(!client||!profile)return null;hydrating=true;
    try{
      const {data,error}=await client.from("cloud_documents").select("collection,row_id,forum_id,owner_user_id,payload,updated_at");
      if(error)throw error;
      await loadProfile();
      const snap=rowsToSnapshot(data);
      snap.v29_users=profiles;
      return snap;
    }finally{hydrating=false}
  }
  function rowFromItem(map,item){
    const rowId=pick(item,map.id);if(!rowId)return null;
    return{collection:map.collection,row_id:String(rowId),forum_id:pick(item,map.forum)||null,owner_user_id:pick(item,map.owner)||null,payload:item,updated_at:new Date().toISOString()}
  }
  async function syncStore(key,value){
    if(!client||!profile||hydrating||!MAP[key])return;
    const map=MAP[key],items=Array.isArray(value)?value:[value],rows=items.map(x=>rowFromItem(map,x)).filter(Boolean);
    if(!rows.length)return;
    const {error}=await client.from("cloud_documents").upsert(rows,{onConflict:"collection,row_id"});
    if(error){console.error("Cloud sync",key,error);emit("error","خطأ مزامنة");return}
    emit("online","تمت المزامنة")
  }
  function onLocalSave(key,value){
    if(!configured()||!client||!profile||!MAP[key]||hydrating)return;
    clearTimeout(writeTimers.get(key));
    writeTimers.set(key,setTimeout(()=>syncStore(key,value),350))
  }
  async function pushAll(snapshot){
    if(!client||!profile)throw new Error("يجب تسجيل الدخول سحابياً أولاً");
    for(const key of Object.keys(MAP)){if(snapshot[key])await syncStore(key,snapshot[key])}
    return true
  }
  async function subscribe(){
    if(!client||!profile||channel)return;
    channel=client.channel("forum-mis-live")
      .on("postgres_changes",{event:"*",schema:"public",table:"cloud_documents"},()=>{clearTimeout(pullTimer);pullTimer=setTimeout(()=>window.dispatchEvent(new Event("forum-mis-cloud-pull")),500)})
      .subscribe();
  }
  window.CloudBridge={configured,init,lookupForum,signIn,signOut,hydrate,pushAll,onLocalSave,legacyCurrent,adminUserAction,changeOwnPassword,getProfiles:()=>profiles.slice(),getProfile:()=>profile};
})();
