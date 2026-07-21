const json=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json; charset=utf-8','cache-control':'no-store'}});
const RESOURCES=new Set(['players','teams','matches','rankings','store-items','purchases','visual-identities','notifications','profiles','wallets','inventory']);

export async function onRequest({request,env,params}){
  if(!env.DB)return json({message:'قاعدة بيانات D1 غير مرتبطة.'},503);
  const parts=(Array.isArray(params.path)?params.path.join('/'):(params.path||'')).split('/').filter(Boolean).map(decodeURIComponent);
  const method=request.method.toUpperCase();
  try{
    await ensureSchema(env.DB);
    const identity=await resolveUser(request,env.DB);
    if(!identity)return json({message:'انتهت الجلسة أو أنها غير صالحة.'},401);
    const ip=request.headers.get('cf-connecting-ip')||'unknown';
    if(!await consumeRateLimit(env.DB,`${identity.userId}:${ip}:v1`,240,60))return json({message:'طلبات كثيرة جدًا. حاول لاحقًا.'},429);

    if(method==='GET'&&parts.length===0)return json({version:'v1',authenticated:true,resources:[...RESOURCES]});
    const resource=parts[0];
    if(!RESOURCES.has(resource))return json({message:'نوع المورد غير مدعوم.'},404);
    const recordId=parts[1]||null;

    if(method==='GET')return recordId?getOne(env.DB,identity.userId,resource,recordId):getAll(env.DB,identity.userId,resource,request);
    if(method==='POST')return createRecord(env.DB,identity,resource,request,ip);
    if(method==='PUT'&&recordId)return upsertRecord(env.DB,identity,resource,recordId,request,ip);
    if(method==='DELETE'&&recordId)return deleteRecord(env.DB,identity,resource,recordId,ip);
    return json({message:'العملية غير مدعومة.'},405);
  }catch(error){console.error(error);return json({message:'حدث خطأ داخلي في API v1.'},500);}
}

async function ensureSchema(db){
  await db.batch([
    db.prepare(`CREATE TABLE IF NOT EXISTS sync_records(application_user_id TEXT NOT NULL,resource_type TEXT NOT NULL,record_id TEXT NOT NULL,payload_json TEXT NOT NULL,revision INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL,updated_at TEXT NOT NULL,deleted_at TEXT,PRIMARY KEY(application_user_id,resource_type,record_id))`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_sync_records_lookup ON sync_records(application_user_id,resource_type,updated_at)`),
    db.prepare(`CREATE TABLE IF NOT EXISTS api_rate_limits(rate_key TEXT PRIMARY KEY,window_started_at INTEGER NOT NULL,request_count INTEGER NOT NULL)`),
    db.prepare(`CREATE TABLE IF NOT EXISTS api_audit_logs(audit_id TEXT PRIMARY KEY,application_user_id TEXT,event_type TEXT NOT NULL,ip_address TEXT,device_id TEXT,created_at TEXT NOT NULL,details_json TEXT NOT NULL)`)
  ]);
}

async function resolveUser(request,db){
  const auth=request.headers.get('authorization')||'';
  if(!auth.toLowerCase().startsWith('bearer '))return null;
  const token=auth.slice(7).trim();
  const deviceId=request.headers.get('x-device-id')||'';
  const row=await db.prepare(`SELECT application_user_id,device_id FROM preview_sessions WHERE token=? AND expires_at>?`).bind(token,new Date().toISOString()).first();
  if(!row||(row.device_id&&row.device_id!==deviceId))return null;
  return {userId:row.application_user_id,deviceId};
}

async function getAll(db,userId,resource,request){
  const url=new URL(request.url);const since=url.searchParams.get('since');const includeDeleted=url.searchParams.get('includeDeleted')==='true';
  let sql=`SELECT record_id AS recordId,payload_json AS payloadJson,revision,created_at AS createdAt,updated_at AS updatedAt,deleted_at AS deletedAt FROM sync_records WHERE application_user_id=? AND resource_type=?`;
  const binds=[userId,resource];if(since){sql+=' AND updated_at>?';binds.push(since);}if(!includeDeleted)sql+=' AND deleted_at IS NULL';sql+=' ORDER BY updated_at DESC LIMIT 500';
  const result=await db.prepare(sql).bind(...binds).all();return json((result.results||[]).map(mapRow));
}
async function getOne(db,userId,resource,recordId){const row=await db.prepare(`SELECT record_id AS recordId,payload_json AS payloadJson,revision,created_at AS createdAt,updated_at AS updatedAt,deleted_at AS deletedAt FROM sync_records WHERE application_user_id=? AND resource_type=? AND record_id=?`).bind(userId,resource,recordId).first();return row?json(mapRow(row)):json({message:'السجل غير موجود.'},404);}
async function createRecord(db,identity,resource,request,ip){const body=await safeBody(request);const recordId=String(body.recordId||body.id||makeId(prefixFor(resource)));const response=await upsertPayload(db,identity.userId,resource,recordId,body.payload??body,false);await audit(db,identity,'sync_create',ip,{resource,recordId});return response;}
async function upsertRecord(db,identity,resource,recordId,request,ip){const body=await safeBody(request);const response=await upsertPayload(db,identity.userId,resource,recordId,body.payload??body,true);await audit(db,identity,'sync_upsert',ip,{resource,recordId});return response;}
async function upsertPayload(db,userId,resource,recordId,payload,allowExisting){const now=new Date().toISOString();const existing=await db.prepare(`SELECT revision,created_at FROM sync_records WHERE application_user_id=? AND resource_type=? AND record_id=?`).bind(userId,resource,recordId).first();if(existing&&!allowExisting)return json({message:'السجل موجود بالفعل.'},409);const revision=(existing?.revision||0)+1;const createdAt=existing?.created_at||now;await db.prepare(`INSERT INTO sync_records(application_user_id,resource_type,record_id,payload_json,revision,created_at,updated_at,deleted_at) VALUES(?,?,?,?,?,?,?,NULL) ON CONFLICT(application_user_id,resource_type,record_id) DO UPDATE SET payload_json=excluded.payload_json,revision=excluded.revision,updated_at=excluded.updated_at,deleted_at=NULL`).bind(userId,resource,recordId,JSON.stringify(payload??{}),revision,createdAt,now).run();return json({recordId,payload,revision,createdAt,updatedAt:now,deletedAt:null},existing?200:201);}
async function deleteRecord(db,identity,resource,recordId,ip){const now=new Date().toISOString();const result=await db.prepare(`UPDATE sync_records SET deleted_at=?,updated_at=?,revision=revision+1 WHERE application_user_id=? AND resource_type=? AND record_id=? AND deleted_at IS NULL`).bind(now,now,identity.userId,resource,recordId).run();if(result.meta?.changes)await audit(db,identity,'sync_delete',ip,{resource,recordId});return result.meta?.changes?new Response(null,{status:204}):json({message:'السجل غير موجود.'},404);}

async function consumeRateLimit(db,key,limit,windowSeconds){const now=Math.floor(Date.now()/1000);const row=await db.prepare('SELECT window_started_at,request_count FROM api_rate_limits WHERE rate_key=?').bind(key).first();if(!row||now-row.window_started_at>=windowSeconds){await db.prepare(`INSERT INTO api_rate_limits(rate_key,window_started_at,request_count) VALUES(?,?,1) ON CONFLICT(rate_key) DO UPDATE SET window_started_at=excluded.window_started_at,request_count=1`).bind(key,now).run();return true;}if(row.request_count>=limit)return false;await db.prepare('UPDATE api_rate_limits SET request_count=request_count+1 WHERE rate_key=?').bind(key).run();return true;}
async function audit(db,identity,eventType,ip,details){await db.prepare(`INSERT INTO api_audit_logs(audit_id,application_user_id,event_type,ip_address,device_id,created_at,details_json) VALUES(?,?,?,?,?,?,?)`).bind(makeId('AUD'),identity.userId,eventType,ip||'',identity.deviceId||'',new Date().toISOString(),JSON.stringify(details||{})).run();}
function mapRow(row){let payload={};try{payload=JSON.parse(row.payloadJson||'{}');}catch{}return {recordId:row.recordId,payload,revision:row.revision,createdAt:row.createdAt,updatedAt:row.updatedAt,deletedAt:row.deletedAt};}
async function safeBody(request){try{return await request.json();}catch{return {};}}
function makeId(prefix){return `${prefix}-${crypto.randomUUID().replaceAll('-','').toUpperCase()}`;}
function prefixFor(resource){return ({players:'PLY',teams:'TEAM',matches:'MAT',rankings:'RNK','store-items':'AST',purchases:'PUR','visual-identities':'VIS',notifications:'NTF',profiles:'PRF',wallets:'WAL',inventory:'INV'})[resource]||'REC';}
