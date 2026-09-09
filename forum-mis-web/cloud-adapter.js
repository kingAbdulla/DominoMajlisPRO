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
  let client=null,profile=null,profiles=[],channel=null,pullTimer=null,writeTimers=new Map(),hydrating=false,flushTimer=null;
  const OFFLINE_SCHEMA=1;
  const RETRY_DELAYS=[1500,5000,15000,30000,60000];
  const nowIso=()=>new Date().toISOString();
  const online=()=>navigator.onLine!==false;
  const networkLikeError=e=>!online()||/fetch|network|offline|connection|timeout/i.test(String(e?.message||e||""));
  const safeJsonParse=(raw,fallback)=>{try{return raw?JSON.parse(raw):fallback}catch(_){return fallback}};
  const authCacheKey=authId=>"v29_cloud_profile_cache:"+String(authId||"anon");
  const AUTH_CACHE_MAX_AGE_MS=7*24*60*60*1000;
  const profileCacheSave=(authId,p)=>localStorage.setItem(authCacheKey(authId),JSON.stringify({cachedAt:Date.now(),profile:p}));
  const profileCacheLoad=authId=>{
    const raw=safeJsonParse(localStorage.getItem(authCacheKey(authId)),null);
    if(!raw)return null;
    if(raw.profile&&Number(raw.cachedAt)>0){
      if(Date.now()-Number(raw.cachedAt)>AUTH_CACHE_MAX_AGE_MS){localStorage.removeItem(authCacheKey(authId));return null}
      return raw.profile;
    }
    // Legacy cache entries are accepted once, then upgraded with a bounded age.
    profileCacheSave(authId,raw);return raw;
  };
  const scopeId=()=>String(profile?.user_id||"anon")+"::"+String(profile?.forum_id||"GLOBAL");
  const queueKey=()=>"v29_sync_queue:"+scopeId();
  const cacheKey=()=>"v29_scoped_cache:"+scopeId();
  const syncMetaKey=()=>"v29_sync_meta:"+scopeId();
  const queueLoad=()=>safeJsonParse(localStorage.getItem(queueKey()),[]);
  const queueSave=q=>localStorage.setItem(queueKey(),JSON.stringify(q));
  const cacheLoad=()=>safeJsonParse(localStorage.getItem(cacheKey()),{schema:OFFLINE_SCHEMA,rows:{},updatedAt:null});
  const cacheSave=x=>localStorage.setItem(cacheKey(),JSON.stringify({...x,schema:OFFLINE_SCHEMA,updatedAt:nowIso()}));
  const metaLoad=()=>safeJsonParse(localStorage.getItem(syncMetaKey()),{lastSyncAt:null,lastError:null});
  const metaSave=patch=>{const next={...metaLoad(),...patch};localStorage.setItem(syncMetaKey(),JSON.stringify(next));return next};
  const rowKey=(collection,rowId)=>collection+"::"+String(rowId);
  const samePayload=(a,b)=>JSON.stringify(a??null)===JSON.stringify(b??null);
  const emitSyncState=(forced=null)=>{
    const q=queueLoad(),conflicts=q.filter(x=>x.status==="CONFLICT").length,pending=q.filter(x=>x.status==="PENDING"||x.status==="RETRY").length,failed=q.filter(x=>x.status==="FAILED").length,meta=metaLoad();
    if(forced){emit(forced.status,forced.text);return}
    if(!online()){emit("offline","وضع دون اتصال"+(pending?" • "+pending+" بانتظار المزامنة":""));return}
    if(conflicts){emit("conflict","تعارض يحتاج مراجعة • "+conflicts);return}
    if(failed){emit("error","فشل المزامنة • "+failed);return}
    if(pending){emit("pending","بانتظار المزامنة • "+pending);return}
    emit("online","تمت المزامنة"+(meta.lastSyncAt?" • "+new Date(meta.lastSyncAt).toLocaleTimeString("ar-IQ",{hour:"2-digit",minute:"2-digit"}):""))
  };
  const updateCacheRow=row=>{
    const cache=cacheLoad(),k=rowKey(row.collection,row.row_id);
    cache.rows[k]={collection:row.collection,row_id:row.row_id,forum_id:row.forum_id??null,owner_user_id:row.owner_user_id??null,payload:row.payload,updated_at:row.updated_at||null,local_version:Number(row.local_version||cache.rows[k]?.local_version||0)};
    cacheSave(cache);
  };
  const removeCacheRow=(collection,rowId)=>{const cache=cacheLoad();delete cache.rows[rowKey(collection,rowId)];cacheSave(cache)};
  const cacheRowsForCollection=collection=>Object.values(cacheLoad().rows||{}).filter(r=>r.collection===collection);
  const enqueueOperation=op=>{
    let q=queueLoad();
    const same=q.find(x=>x.collection===op.collection&&x.rowId===op.rowId&&["PENDING","RETRY"].includes(x.status));
    if(same){
      if(same.operation==="CREATE"&&op.operation==="UPDATE") op.operation="CREATE";
      if(op.operation==="DELETE"&&same.operation==="CREATE"){q=q.filter(x=>x!==same);queueSave(q);emitSyncState();return}
      Object.assign(same,op,{queueId:same.queueId,status:"PENDING",retries:0,lastError:null,timestamp:nowIso()});
    }else q.push({queueId:crypto.randomUUID?crypto.randomUUID():"Q-"+Date.now()+"-"+Math.random().toString(36).slice(2),status:"PENDING",retries:0,lastError:null,...op,timestamp:op.timestamp||nowIso()});
    queueSave(q);emitSyncState();
  };
  const snapshotFromScopedCache=()=>rowsToSnapshot(Object.values(cacheLoad().rows||{}));

  const pick=(o,keys)=>{for(const k of keys||[])if(o&&o[k])return o[k];return null};
  const slug=v=>String(v||"").trim().toLowerCase().replace(/[^a-z0-9._-]/g,"-");
  const emailForLogin=login=>String(login||"").includes("@")?String(login).trim():slug(login)+"@"+(cfg.loginDomain||"forum-mis.local");
  const configured=()=>!!(cfg.enabled&&cfg.url&&cfg.publishableKey&&window.supabase?.createClient);
  function emit(status,text){window.dispatchEvent(new CustomEvent("forum-mis-cloud-status",{detail:{status,text}}))}
  async function init(){
    if(!configured()){emit("offline","محلي");return false}
    client=window.supabase.createClient(cfg.url,cfg.publishableKey,{auth:{persistSession:true,autoRefreshToken:true,detectSessionInUrl:true}});
    const {data}=await client.auth.getSession();
    if(data?.session){
      try{await loadProfile();await subscribe();scheduleFlush(200)}
      catch(e){if(!networkLikeError(e))throw e;console.warn("Cloud profile unavailable; attempting offline profile cache",e);const cached=profileCacheLoad(data.session.user.id);if(cached){profile=cached;profiles=[legacyProfile(cached)];emitSyncState()}else throw e}
    }else emit("ready","السحابة جاهزة");
    return true
  }
  async function lookupForum(code){
    if(!configured())return null;if(!client)await init();
    const {data,error}=await client.rpc("lookup_forum",{p_code:String(code||"").trim()});
    if(error)throw error;const r=Array.isArray(data)?data[0]:data;
    return r?{forumId:r.forum_id,id:r.forum_id,name:r.name,code:String(code),active:true}:null
  }
  const legacyProfile=p=>({id:p.user_id,userId:p.user_id,login:p.login,name:p.full_name,role:p.role,forum:p.forum_id,forumId:p.forum_id,status:p.status,disabled:!!p.disabled,lastLoginAt:p.last_login_at,mustChangePassword:!!p.must_change_password,passwordChangedAt:p.password_changed_at,temporaryPasswordIssuedAt:p.temporary_password_issued_at,temporaryPasswordExpiresAt:p.temporary_password_expires_at,passwordResetByUserId:p.password_reset_by_user_id,authUserId:p.auth_user_id});
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
    profileCacheSave(user.id,data);

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
    scheduleFlush(50);
    emitSyncState();
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
      if(!online()){
        const snap=snapshotFromScopedCache();snap.v29_users=profiles;emitSyncState();return snap;
      }
      await flushQueue();
      const {data,error}=await client.from("cloud_documents").select("collection,row_id,forum_id,owner_user_id,payload,updated_at");
      if(error)throw error;
      const cache={schema:OFFLINE_SCHEMA,rows:{},updatedAt:nowIso()};
      for(const row of data||[])cache.rows[rowKey(row.collection,row.row_id)]={...row,local_version:0};
      for(const item of queueLoad().filter(x=>["PENDING","RETRY","FAILED","CONFLICT","SYNCING"].includes(x.status))){
        const k=rowKey(item.collection,item.rowId);
        if(item.operation==="DELETE")delete cache.rows[k];
        else cache.rows[k]={collection:item.collection,row_id:item.rowId,forum_id:item.forumId||null,owner_user_id:item.userId||null,payload:item.payload,updated_at:item.baseUpdatedAt||cache.rows[k]?.updated_at||null,local_version:Number(item.localVersion||1)};
      }
      cacheSave(cache);
      await loadProfile();
      const snap=rowsToSnapshot(Object.values(cache.rows||{}));
      snap.v29_users=profiles;
      metaSave({lastSyncAt:nowIso(),lastError:null});
      emitSyncState();
      return snap;
    }catch(e){
      const cached=Object.values(cacheLoad().rows||{});
      if(cached.length){console.warn("Cloud hydrate failed; using scoped offline cache",e);const snap=rowsToSnapshot(cached);snap.v29_users=profiles;emit("offline","وضع دون اتصال • بيانات محلية");return snap}
      throw e;
    }finally{hydrating=false}
  }
  function rowFromItem(map,item){
    const rowId=pick(item,map.id);if(!rowId)return null;
    return{collection:map.collection,row_id:String(rowId),forum_id:pick(item,map.forum)||null,owner_user_id:pick(item,map.owner)||null,payload:item,updated_at:new Date().toISOString()}
  }
  function buildQueueOps(key,value){
    const map=MAP[key],items=Array.isArray(value)?value:[value],nextRows=items.map(x=>rowFromItem(map,x)).filter(Boolean);
    const cached=cacheRowsForCollection(map.collection),cachedById=new Map(cached.map(r=>[String(r.row_id),r])),nextById=new Map(nextRows.map(r=>[String(r.row_id),r]));
    const userId=profile?.user_id||null;
    for(const row of nextRows){
      const prev=cachedById.get(String(row.row_id));
      if(prev&&samePayload(prev.payload,row.payload))continue;
      const localVersion=Number(prev?.local_version||0)+1;
      const operation=prev?"UPDATE":"CREATE";
      enqueueOperation({operation,collection:row.collection,rowId:String(row.row_id),entityId:String(row.row_id),forumId:row.forum_id||profile?.forum_id||null,userId,localVersion,baseUpdatedAt:prev?.updated_at||null,payload:row.payload});
      updateCacheRow({...row,updated_at:prev?.updated_at||null,local_version:localVersion});
    }
    for(const prev of cached){
      if(nextById.has(String(prev.row_id)))continue;
      enqueueOperation({operation:"DELETE",collection:prev.collection,rowId:String(prev.row_id),entityId:String(prev.row_id),forumId:prev.forum_id||profile?.forum_id||null,userId,localVersion:Number(prev.local_version||0)+1,baseUpdatedAt:prev.updated_at||null,payload:null});
      removeCacheRow(prev.collection,prev.row_id);
    }
  }
  async function processQueueItem(item){
    const q=queueLoad(),target=q.find(x=>x.queueId===item.queueId);if(!target)return;
    target.status="SYNCING";queueSave(q);emit("syncing","يتم الرفع");
    try{
      const {data:remote,error:readError}=await client.from("cloud_documents").select("collection,row_id,forum_id,owner_user_id,payload,updated_at").eq("collection",item.collection).eq("row_id",item.rowId).maybeSingle();
      if(readError)throw readError;
      const base=item.baseUpdatedAt?new Date(item.baseUpdatedAt).getTime():null,remoteTime=remote?.updated_at?new Date(remote.updated_at).getTime():null;
      const concurrent=!!remote && ((item.operation==="CREATE") || (base!==null&&remoteTime!==base));
      if(concurrent&&!samePayload(remote?.payload,item.payload)){
        target.status="CONFLICT";target.remoteSnapshot=remote;target.lastError="تم تعديل السجل على جهاز آخر بعد النسخة المحلية.";queueSave(q);emitSyncState();return;
      }
      if(item.operation==="DELETE"){
        if(remote){const {error}=await client.from("cloud_documents").delete().eq("collection",item.collection).eq("row_id",item.rowId);if(error)throw error}
        removeCacheRow(item.collection,item.rowId);
      }else{
        const row={collection:item.collection,row_id:item.rowId,forum_id:item.forumId||null,owner_user_id:item.userId||null,payload:item.payload,updated_at:nowIso()};
        const {data:saved,error}=await client.from("cloud_documents").upsert(row,{onConflict:"collection,row_id"}).select("collection,row_id,forum_id,owner_user_id,payload,updated_at").single();
        if(error)throw error;updateCacheRow(saved||row);
      }
      const fresh=queueLoad().filter(x=>x.queueId!==item.queueId);queueSave(fresh);metaSave({lastSyncAt:nowIso(),lastError:null});emitSyncState();
    }catch(e){
      const fresh=queueLoad(),x=fresh.find(v=>v.queueId===item.queueId);if(!x)return;
      x.retries=Number(x.retries||0)+1;x.lastError=e?.message||String(e);x.status=x.retries>=RETRY_DELAYS.length?"FAILED":"RETRY";queueSave(fresh);metaSave({lastError:x.lastError});emitSyncState();
      if(x.status==="RETRY")scheduleFlush(RETRY_DELAYS[Math.min(x.retries-1,RETRY_DELAYS.length-1)]);
    }
  }
  const officialRecord=item=>{
    if(item.collection==="audit_events")return true;
    if(item.collection!=="reports")return false;
    const p=item.payload||{},r=item.remoteSnapshot?.payload||{};
    const status=String(p.statusCode||p.status||r.statusCode||r.status||"");
    return /Issued|Approved|صادر|معتمد|Revoked|ملغى/i.test(status);
  };
  const canResolveConflict=item=>{
    if(!profile||!item||item.status!=="CONFLICT")return false;
    if(["المطور","مدير الإدارة"].includes(profile.role))return true;
    if(profile.role==="مدير المنتدى"&&!officialRecord(item)){
      return !item.forumId||item.forumId===profile.forum_id;
    }
    return false;
  };
  async function resolveConflict(queueId,decision){
    if(!client||!profile)throw new Error("يجب تسجيل الدخول أولاً");
    if(!online())throw new Error("يلزم الاتصال بالإنترنت لحسم التعارض.");
    const q=queueLoad(),item=q.find(x=>x.queueId===queueId);
    if(!item||item.status!=="CONFLICT")throw new Error("سجل التعارض غير موجود.");
    if(!canResolveConflict(item))throw new Error("ليست لديك صلاحية حسم هذا التعارض.");
    if(!["LOCAL","CLOUD"].includes(decision))throw new Error("قرار حسم غير صالح.");

    if(decision==="CLOUD"){
      if(item.remoteSnapshot)updateCacheRow(item.remoteSnapshot);
      else removeCacheRow(item.collection,item.rowId);
      queueSave(q.filter(x=>x.queueId!==queueId));
      metaSave({lastSyncAt:nowIso(),lastError:null});
      emitSyncState();
      window.dispatchEvent(new CustomEvent("forum-mis-conflict-resolved",{detail:{queueId,decision,collection:item.collection,rowId:item.rowId,official:officialRecord(item)}}));
      window.dispatchEvent(new Event("forum-mis-cloud-pull"));
      return {ok:true,decision};
    }

    const row={collection:item.collection,row_id:item.rowId,forum_id:item.forumId||null,owner_user_id:item.userId||null,payload:item.payload,updated_at:nowIso()};
    const {data:saved,error}=await client.from("cloud_documents").upsert(row,{onConflict:"collection,row_id"}).select("collection,row_id,forum_id,owner_user_id,payload,updated_at").single();
    if(error)throw error;
    updateCacheRow(saved||row);
    queueSave(q.filter(x=>x.queueId!==queueId));
    metaSave({lastSyncAt:nowIso(),lastError:null});
    emitSyncState();
    window.dispatchEvent(new CustomEvent("forum-mis-conflict-resolved",{detail:{queueId,decision,collection:item.collection,rowId:item.rowId,official:officialRecord(item)}}));
    window.dispatchEvent(new Event("forum-mis-cloud-pull"));
    return {ok:true,decision};
  }

  async function flushQueue(){
    if(!client||!profile||!online())return emitSyncState();
    clearTimeout(flushTimer);flushTimer=null;
    const q=queueLoad().filter(x=>["PENDING","RETRY"].includes(x.status));
    for(const item of q){if(!online())break;await processQueueItem(item)}
    emitSyncState();
  }
  function scheduleFlush(delay=300){clearTimeout(flushTimer);flushTimer=setTimeout(()=>flushQueue().catch(e=>{console.error("Queue flush",e);metaSave({lastError:e?.message||String(e)});emitSyncState()}),delay)}
  async function syncStore(key,value){
    if(!client||!profile||hydrating||!MAP[key])return;
    buildQueueOps(key,value);
    if(online())scheduleFlush(50);else emitSyncState();
  }
  function onLocalSave(key,value){
    if(!configured()||!client||!profile||!MAP[key]||hydrating)return;
    clearTimeout(writeTimers.get(key));
    writeTimers.set(key,setTimeout(()=>syncStore(key,value),250))
  }
  async function pushAll(snapshot){
    if(!client||!profile)throw new Error("يجب تسجيل الدخول سحابياً أولاً");
    for(const key of Object.keys(MAP)){if(snapshot[key]!==undefined)buildQueueOps(key,snapshot[key])}
    if(online())await flushQueue();else emitSyncState();
    return true
  }
  async function subscribe(){
    if(!client||!profile||channel)return;
    channel=client.channel("forum-mis-live")
      .on("postgres_changes",{event:"*",schema:"public",table:"cloud_documents"},()=>{clearTimeout(pullTimer);pullTimer=setTimeout(async()=>{await flushQueue();if(!queueLoad().some(x=>["PENDING","RETRY","SYNCING"].includes(x.status)))window.dispatchEvent(new Event("forum-mis-cloud-pull"))},500)})
      .subscribe();
  }
  window.addEventListener("online",()=>{emit("pending","بانتظار المزامنة");scheduleFlush(100)});
  window.addEventListener("offline",()=>emitSyncState());
  window.CloudBridge={configured,init,lookupForum,signIn,signOut,hydrate,pushAll,onLocalSave,legacyCurrent,adminUserAction,changeOwnPassword,flushQueue,resolveConflict,canResolveConflict,getSyncQueue:()=>queueLoad().slice(),getSyncState:()=>({online:online(),queue:queueLoad().slice(),meta:metaLoad(),scope:scopeId()}),getProfiles:()=>profiles.slice(),getProfile:()=>profile};
})();
