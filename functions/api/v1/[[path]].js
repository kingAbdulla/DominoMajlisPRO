const json=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json; charset=utf-8','cache-control':'no-store'}});

const RESOURCES=new Set([
  'players','teams','matches','rankings','store-items','purchases',
  'visual-identities','notifications','profiles','wallets','inventory'
]);

export async function onRequest({request,env,params}){
  if(!env.DB)return json({message:'قاعدة بيانات D1 غير مرتبطة.'},503);
  const parts=(Array.isArray(params.path)?params.path.join('/'):(params.path||''))
    .split('/').filter(Boolean).map(decodeURIComponent);
  const method=request.method.toUpperCase();
  try{
    await ensureSchema(env.DB);
    const userId=await resolveUser(request,env.DB);
    if(!userId)return json({message:'انتهت الجلسة أو أنها غير صالحة.'},401);

    if(method==='GET'&&parts.length===0)return json({version:'v1',authenticated:true,resources:[...RESOURCES]});
    const resource=parts[0];
    if(!RESOURCES.has(resource))return json({message:'نوع المورد غير مدعوم.'},404);
    const recordId=parts[1]||null;

    if(method==='GET')return recordId?getOne(env.DB,userId,resource,recordId):getAll(env.DB,userId,resource,request);
    if(method==='POST')return createRecord(env.DB,userId,resource,request);
    if(method==='PUT'&&recordId)return upsertRecord(env.DB,userId,resource,recordId,request);
    if(method==='DELETE'&&recordId)return deleteRecord(env.DB,userId,resource,recordId);
    return json({message:'العملية غير مدعومة.'},405);
  }catch(error){
    console.error(error);
    return json({message:'حدث خطأ داخلي في API v1.'},500);
  }
}

async function ensureSchema(db){
  await db.batch([
    db.prepare(`CREATE TABLE IF NOT EXISTS sync_records(
      application_user_id TEXT NOT NULL,
      resource_type TEXT NOT NULL,
      record_id TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      revision INTEGER NOT NULL DEFAULT 1,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL,
      deleted_at TEXT,
      PRIMARY KEY(application_user_id,resource_type,record_id)
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_sync_records_lookup
      ON sync_records(application_user_id,resource_type,updated_at)`)
  ]);
}

async function resolveUser(request,db){
  const auth=request.headers.get('authorization')||'';
  if(!auth.toLowerCase().startsWith('bearer '))return null;
  const token=auth.slice(7).trim();
  const row=await db.prepare(`SELECT application_user_id FROM preview_sessions
    WHERE token=? AND expires_at>?`).bind(token,new Date().toISOString()).first();
  return row?.application_user_id||null;
}

async function getAll(db,userId,resource,request){
  const url=new URL(request.url);
  const since=url.searchParams.get('since');
  const includeDeleted=url.searchParams.get('includeDeleted')==='true';
  let sql=`SELECT record_id AS recordId,payload_json AS payloadJson,revision,created_at AS createdAt,updated_at AS updatedAt,deleted_at AS deletedAt
    FROM sync_records WHERE application_user_id=? AND resource_type=?`;
  const binds=[userId,resource];
  if(since){sql+=' AND updated_at>?';binds.push(since);}
  if(!includeDeleted)sql+=' AND deleted_at IS NULL';
  sql+=' ORDER BY updated_at DESC LIMIT 500';
  const result=await db.prepare(sql).bind(...binds).all();
  return json((result.results||[]).map(mapRow));
}

async function getOne(db,userId,resource,recordId){
  const row=await db.prepare(`SELECT record_id AS recordId,payload_json AS payloadJson,revision,created_at AS createdAt,updated_at AS updatedAt,deleted_at AS deletedAt
    FROM sync_records WHERE application_user_id=? AND resource_type=? AND record_id=?`)
    .bind(userId,resource,recordId).first();
  return row?json(mapRow(row)):json({message:'السجل غير موجود.'},404);
}

async function createRecord(db,userId,resource,request){
  const body=await safeBody(request);
  const recordId=String(body.recordId||body.id||makeId(prefixFor(resource)));
  return upsertPayload(db,userId,resource,recordId,body.payload??body,false);
}

async function upsertRecord(db,userId,resource,recordId,request){
  const body=await safeBody(request);
  return upsertPayload(db,userId,resource,recordId,body.payload??body,true);
}

async function upsertPayload(db,userId,resource,recordId,payload,allowExisting){
  const now=new Date().toISOString();
  const existing=await db.prepare(`SELECT revision,created_at FROM sync_records
    WHERE application_user_id=? AND resource_type=? AND record_id=?`).bind(userId,resource,recordId).first();
  if(existing&&!allowExisting)return json({message:'السجل موجود بالفعل.'},409);
  const revision=(existing?.revision||0)+1;
  const createdAt=existing?.created_at||now;
  await db.prepare(`INSERT INTO sync_records
    (application_user_id,resource_type,record_id,payload_json,revision,created_at,updated_at,deleted_at)
    VALUES(?,?,?,?,?,?,?,NULL)
    ON CONFLICT(application_user_id,resource_type,record_id) DO UPDATE SET
      payload_json=excluded.payload_json,revision=excluded.revision,updated_at=excluded.updated_at,deleted_at=NULL`)
    .bind(userId,resource,recordId,JSON.stringify(payload??{}),revision,createdAt,now).run();
  return json({recordId,payload,revision,createdAt,updatedAt:now,deletedAt:null},existing?200:201);
}

async function deleteRecord(db,userId,resource,recordId){
  const now=new Date().toISOString();
  const result=await db.prepare(`UPDATE sync_records SET deleted_at=?,updated_at=?,revision=revision+1
    WHERE application_user_id=? AND resource_type=? AND record_id=? AND deleted_at IS NULL`)
    .bind(now,now,userId,resource,recordId).run();
  return result.meta?.changes?new Response(null,{status:204}):json({message:'السجل غير موجود.'},404);
}

function mapRow(row){
  let payload={};try{payload=JSON.parse(row.payloadJson||'{}');}catch{}
  return {recordId:row.recordId,payload,revision:row.revision,createdAt:row.createdAt,updatedAt:row.updatedAt,deletedAt:row.deletedAt};
}
async function safeBody(request){try{return await request.json();}catch{return {};}}
function makeId(prefix){return `${prefix}-${crypto.randomUUID().replaceAll('-','').toUpperCase()}`;}
function prefixFor(resource){return ({players:'PLY',teams:'TEAM',matches:'MAT',rankings:'RNK','store-items':'AST',purchases:'PUR','visual-identities':'VIS',notifications:'NTF',profiles:'PRF',wallets:'WAL',inventory:'INV'})[resource]||'REC';}
