const json = (data, status = 200) => new Response(JSON.stringify(data), {
  status,
  headers: { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' }
});

export async function onRequest(context) {
  const { request, env, params } = context;
  const path = Array.isArray(params.path) ? params.path.join('/') : (params.path || '');
  const method = request.method.toUpperCase();

  if (method === 'GET' && path === 'health') {
    return json({ service: 'DominoMajlisPRO.Cloudflare', status: 'healthy', database: Boolean(env.DB), utc: new Date().toISOString() });
  }

  if (!env.DB) return json({ message: 'قاعدة بيانات D1 غير مرتبطة بالمشروع بعد.' }, 503);

  try {
    await ensureSchema(env.DB);
    if (method === 'POST' && path === 'preview/register') return await register(request, env.DB);
    if (method === 'POST' && path === 'preview/login') return await login(request, env.DB);
    if (method === 'POST' && path === 'preview/logout') return await logout(request, env.DB);
    if (method === 'GET' && path === 'preview/me/teams') return await getTeams(request, env.DB);
    if (method === 'POST' && path === 'preview/me/teams') return await createTeam(request, env.DB);
    return json({ message: 'المسار غير موجود.' }, 404);
  } catch (error) {
    console.error(error);
    return json({ message: 'حدث خطأ داخلي في السيرفر التجريبي.' }, 500);
  }
}

async function ensureSchema(db) {
  await db.batch([
    db.prepare(`CREATE TABLE IF NOT EXISTS preview_users (
      application_user_id TEXT PRIMARY KEY,
      player_id TEXT NOT NULL UNIQUE,
      username TEXT NOT NULL,
      username_norm TEXT NOT NULL UNIQUE,
      password_salt TEXT NOT NULL,
      password_hash TEXT NOT NULL,
      created_at TEXT NOT NULL
    )`),
    db.prepare(`CREATE TABLE IF NOT EXISTS preview_sessions (
      token TEXT PRIMARY KEY,
      application_user_id TEXT NOT NULL,
      expires_at TEXT NOT NULL,
      created_at TEXT NOT NULL
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_preview_sessions_user
      ON preview_sessions(application_user_id)`),
    db.prepare(`CREATE TABLE IF NOT EXISTS preview_teams (
      team_id TEXT PRIMARY KEY,
      application_user_id TEXT NOT NULL,
      name TEXT NOT NULL,
      created_at TEXT NOT NULL
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_preview_teams_user
      ON preview_teams(application_user_id, created_at)`)
  ]);
}

async function register(request, db) {
  const body = await safeBody(request);
  const username = String(body.username || '').trim();
  const password = String(body.password || '');
  if (username.length < 3 || password.length < 8) return json({ message: 'اسم المستخدم 3 أحرف على الأقل وكلمة المرور 8 أحرف على الأقل.' }, 400);

  const existing = await db.prepare('SELECT 1 FROM preview_users WHERE username_norm = ?').bind(username.toLowerCase()).first();
  if (existing) return json({ message: 'اسم المستخدم مستخدم بالفعل.' }, 409);

  const salt = crypto.getRandomValues(new Uint8Array(16));
  const hash = await passwordHash(password, salt);
  const applicationUserId = id('USR');
  const playerId = id('PLY');
  const now = new Date().toISOString();
  await db.prepare(`INSERT INTO preview_users
    (application_user_id, player_id, username, username_norm, password_salt, password_hash, created_at)
    VALUES (?, ?, ?, ?, ?, ?, ?)`) 
    .bind(applicationUserId, playerId, username, username.toLowerCase(), toBase64(salt), toBase64(hash), now).run();

  return createSession(db, { applicationUserId, playerId, displayName: username });
}

async function login(request, db) {
  const body = await safeBody(request);
  const username = String(body.username || '').trim();
  const password = String(body.password || '');
  const row = await db.prepare(`SELECT application_user_id, player_id, username, password_salt, password_hash
    FROM preview_users WHERE username_norm = ?`).bind(username.toLowerCase()).first();
  if (!row) return json({ message: 'بيانات الدخول غير صحيحة.' }, 401);

  const actual = await passwordHash(password, fromBase64(row.password_salt));
  if (!timingSafeEqual(actual, fromBase64(row.password_hash))) return json({ message: 'بيانات الدخول غير صحيحة.' }, 401);

  return createSession(db, { applicationUserId: row.application_user_id, playerId: row.player_id, displayName: row.username });
}

async function logout(request, db) {
  const token = bearer(request);
  if (!token) return json({ message: 'الجلسة غير صالحة.' }, 401);
  await db.prepare('DELETE FROM preview_sessions WHERE token = ?').bind(token).run();
  return new Response(null, { status: 204 });
}

async function getTeams(request, db) {
  const userId = await resolveUser(request, db);
  if (!userId) return json({ message: 'انتهت الجلسة أو أنها غير صالحة.' }, 401);
  const result = await db.prepare(`SELECT team_id AS teamId, name, created_at AS createdAt
    FROM preview_teams WHERE application_user_id = ? ORDER BY created_at DESC`).bind(userId).all();
  return json(result.results || []);
}

async function createTeam(request, db) {
  const userId = await resolveUser(request, db);
  if (!userId) return json({ message: 'انتهت الجلسة أو أنها غير صالحة.' }, 401);
  const body = await safeBody(request);
  const name = String(body.name || '').trim();
  if (!name) return json({ message: 'اسم الفريق مطلوب.' }, 400);
  const team = { teamId: id('TEAM'), name, createdAt: new Date().toISOString() };
  await db.prepare(`INSERT INTO preview_teams (team_id, application_user_id, name, created_at)
    VALUES (?, ?, ?, ?)`).bind(team.teamId, userId, team.name, team.createdAt).run();
  return json(team, 201);
}

async function createSession(db, user) {
  const token = hex(crypto.getRandomValues(new Uint8Array(32)));
  const expiresAt = new Date(Date.now() + 12 * 60 * 60 * 1000).toISOString();
  await db.prepare(`INSERT INTO preview_sessions (token, application_user_id, expires_at, created_at)
    VALUES (?, ?, ?, ?)`).bind(token, user.applicationUserId, expiresAt, new Date().toISOString()).run();
  return json({ accessToken: token, expiresAt, user });
}

async function resolveUser(request, db) {
  const token = bearer(request);
  if (!token) return null;
  const row = await db.prepare(`SELECT application_user_id FROM preview_sessions
    WHERE token = ? AND expires_at > ?`).bind(token, new Date().toISOString()).first();
  return row?.application_user_id || null;
}

async function passwordHash(password, salt) {
  const key = await crypto.subtle.importKey('raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveBits']);
  const bits = await crypto.subtle.deriveBits({ name: 'PBKDF2', hash: 'SHA-256', salt, iterations: 120000 }, key, 256);
  return new Uint8Array(bits);
}

function timingSafeEqual(a, b) {
  if (a.length !== b.length) return false;
  let value = 0;
  for (let i = 0; i < a.length; i++) value |= a[i] ^ b[i];
  return value === 0;
}

function bearer(request) {
  const value = request.headers.get('authorization') || '';
  return value.toLowerCase().startsWith('bearer ') ? value.slice(7).trim() : null;
}

async function safeBody(request) {
  try { return await request.json(); } catch { return {}; }
}

function id(prefix) { return `${prefix}-${crypto.randomUUID().replaceAll('-', '').toUpperCase()}`; }
function hex(bytes) { return [...bytes].map(x => x.toString(16).padStart(2, '0')).join('').toUpperCase(); }
function toBase64(bytes) { return btoa(String.fromCharCode(...bytes)); }
function fromBase64(value) { return Uint8Array.from(atob(value), c => c.charCodeAt(0)); }
