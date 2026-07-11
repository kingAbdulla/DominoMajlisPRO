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
    const ip = request.headers.get('cf-connecting-ip') || 'unknown';
    const limited = await consumeRateLimit(env.DB, `${ip}:${path}`, path.includes('login') || path.includes('register') ? 12 : 120, 60);
    if (!limited) return json({ message: 'طلبات كثيرة جدًا. حاول لاحقًا.' }, 429);

    if (method === 'POST' && path === 'preview/register') return await register(request, env.DB, ip);
    if (method === 'POST' && path === 'preview/login') return await login(request, env.DB, ip);
    if (method === 'POST' && path === 'preview/refresh') return await refresh(request, env.DB, ip);
    if (method === 'POST' && path === 'preview/logout') return await logout(request, env.DB, ip);
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
      application_user_id TEXT PRIMARY KEY, player_id TEXT NOT NULL UNIQUE,
      username TEXT NOT NULL, username_norm TEXT NOT NULL UNIQUE,
      password_salt TEXT NOT NULL, password_hash TEXT NOT NULL, created_at TEXT NOT NULL
    )`),
    db.prepare(`CREATE TABLE IF NOT EXISTS preview_sessions (
      token TEXT PRIMARY KEY, application_user_id TEXT NOT NULL,
      device_id TEXT NOT NULL DEFAULT '', expires_at TEXT NOT NULL, created_at TEXT NOT NULL
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_preview_sessions_user ON preview_sessions(application_user_id)`),
    db.prepare(`CREATE TABLE IF NOT EXISTS preview_refresh_tokens (
      refresh_token TEXT PRIMARY KEY, application_user_id TEXT NOT NULL,
      device_id TEXT NOT NULL DEFAULT '', expires_at TEXT NOT NULL,
      created_at TEXT NOT NULL, revoked_at TEXT
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_preview_refresh_user ON preview_refresh_tokens(application_user_id)`),
    db.prepare(`CREATE TABLE IF NOT EXISTS preview_teams (
      team_id TEXT PRIMARY KEY, application_user_id TEXT NOT NULL,
      name TEXT NOT NULL, created_at TEXT NOT NULL
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_preview_teams_user ON preview_teams(application_user_id, created_at)`),
    db.prepare(`CREATE TABLE IF NOT EXISTS api_rate_limits (
      rate_key TEXT PRIMARY KEY, window_started_at INTEGER NOT NULL, request_count INTEGER NOT NULL
    )`),
    db.prepare(`CREATE TABLE IF NOT EXISTS api_audit_logs (
      audit_id TEXT PRIMARY KEY, application_user_id TEXT,
      event_type TEXT NOT NULL, ip_address TEXT, device_id TEXT,
      created_at TEXT NOT NULL, details_json TEXT NOT NULL
    )`),
    db.prepare(`CREATE INDEX IF NOT EXISTS ix_audit_user_time ON api_audit_logs(application_user_id, created_at)`)
  ]);

  try { await db.prepare(`ALTER TABLE preview_sessions ADD COLUMN device_id TEXT NOT NULL DEFAULT ''`).run(); } catch {}
}

async function register(request, db, ip) {
  const body = await safeBody(request);
  const username = String(body.username || '').trim();
  const password = String(body.password || '');
  const deviceId = device(request, body);
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

  await audit(db, applicationUserId, 'register', ip, deviceId, { username });
  return createSession(db, { applicationUserId, playerId, displayName: username }, deviceId);
}

async function login(request, db, ip) {
  const body = await safeBody(request);
  const username = String(body.username || '').trim();
  const password = String(body.password || '');
  const deviceId = device(request, body);
  const row = await db.prepare(`SELECT application_user_id, player_id, username, password_salt, password_hash
    FROM preview_users WHERE username_norm = ?`).bind(username.toLowerCase()).first();
  if (!row) {
    await audit(db, null, 'login_failed', ip, deviceId, { username });
    return json({ message: 'بيانات الدخول غير صحيحة.' }, 401);
  }

  const actual = await passwordHash(password, fromBase64(row.password_salt));
  if (!timingSafeEqual(actual, fromBase64(row.password_hash))) {
    await audit(db, row.application_user_id, 'login_failed', ip, deviceId, { username });
    return json({ message: 'بيانات الدخول غير صحيحة.' }, 401);
  }

  await audit(db, row.application_user_id, 'login', ip, deviceId, {});
  return createSession(db, { applicationUserId: row.application_user_id, playerId: row.player_id, displayName: row.username }, deviceId);
}

async function refresh(request, db, ip) {
  const body = await safeBody(request);
  const refreshToken = String(body.refreshToken || '');
  const deviceId = device(request, body);
  if (!refreshToken) return json({ message: 'رمز التحديث مطلوب.' }, 400);

  const row = await db.prepare(`SELECT r.application_user_id, r.device_id, u.player_id, u.username
    FROM preview_refresh_tokens r JOIN preview_users u ON u.application_user_id=r.application_user_id
    WHERE r.refresh_token=? AND r.revoked_at IS NULL AND r.expires_at>?`)
    .bind(refreshToken, new Date().toISOString()).first();
  if (!row || (row.device_id && row.device_id !== deviceId)) return json({ message: 'رمز التحديث غير صالح.' }, 401);

  await db.prepare(`UPDATE preview_refresh_tokens SET revoked_at=? WHERE refresh_token=?`)
    .bind(new Date().toISOString(), refreshToken).run();
  await audit(db, row.application_user_id, 'token_refresh', ip, deviceId, {});
  return createSession(db, {
    applicationUserId: row.application_user_id,
    playerId: row.player_id,
    displayName: row.username
  }, deviceId);
}

async function logout(request, db, ip) {
  const token = bearer(request);
  if (!token) return json({ message: 'الجلسة غير صالحة.' }, 401);
  const row = await db.prepare('SELECT application_user_id, device_id FROM preview_sessions WHERE token=?').bind(token).first();
  await db.prepare('DELETE FROM preview_sessions WHERE token = ?').bind(token).run();
  if (row) {
    await db.prepare(`UPDATE preview_refresh_tokens SET revoked_at=?
      WHERE application_user_id=? AND device_id=? AND revoked_at IS NULL`)
      .bind(new Date().toISOString(), row.application_user_id, row.device_id || '').run();
    await audit(db, row.application_user_id, 'logout', ip, row.device_id || '', {});
  }
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

async function createSession(db, user, deviceId) {
  const token = hex(crypto.getRandomValues(new Uint8Array(32)));
  const refreshToken = hex(crypto.getRandomValues(new Uint8Array(48)));
  const now = new Date();
  const expiresAt = new Date(now.getTime() + 12 * 60 * 60 * 1000).toISOString();
  const refreshExpiresAt = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString();
  await db.batch([
    db.prepare(`INSERT INTO preview_sessions (token, application_user_id, device_id, expires_at, created_at)
      VALUES (?, ?, ?, ?, ?)`).bind(token, user.applicationUserId, deviceId, expiresAt, now.toISOString()),
    db.prepare(`INSERT INTO preview_refresh_tokens
      (refresh_token, application_user_id, device_id, expires_at, created_at, revoked_at)
      VALUES (?, ?, ?, ?, ?, NULL)`).bind(refreshToken, user.applicationUserId, deviceId, refreshExpiresAt, now.toISOString())
  ]);
  return json({ accessToken: token, refreshToken, expiresAt, refreshExpiresAt, user });
}

async function resolveUser(request, db) {
  const token = bearer(request);
  if (!token) return null;
  const deviceId = request.headers.get('x-device-id') || '';
  const row = await db.prepare(`SELECT application_user_id, device_id FROM preview_sessions
    WHERE token = ? AND expires_at > ?`).bind(token, new Date().toISOString()).first();
  if (!row || (row.device_id && row.device_id !== deviceId)) return null;
  return row.application_user_id || null;
}

async function consumeRateLimit(db, key, limit, windowSeconds) {
  const now = Math.floor(Date.now() / 1000);
  const row = await db.prepare('SELECT window_started_at, request_count FROM api_rate_limits WHERE rate_key=?').bind(key).first();
  if (!row || now - row.window_started_at >= windowSeconds) {
    await db.prepare(`INSERT INTO api_rate_limits(rate_key,window_started_at,request_count) VALUES(?,?,1)
      ON CONFLICT(rate_key) DO UPDATE SET window_started_at=excluded.window_started_at,request_count=1`)
      .bind(key, now).run();
    return true;
  }
  if (row.request_count >= limit) return false;
  await db.prepare('UPDATE api_rate_limits SET request_count=request_count+1 WHERE rate_key=?').bind(key).run();
  return true;
}

async function audit(db, userId, eventType, ip, deviceId, details) {
  await db.prepare(`INSERT INTO api_audit_logs
    (audit_id,application_user_id,event_type,ip_address,device_id,created_at,details_json)
    VALUES(?,?,?,?,?,?,?)`)
    .bind(id('AUD'), userId, eventType, ip || '', deviceId || '', new Date().toISOString(), JSON.stringify(details || {})).run();
}

async function passwordHash(password, salt) {
  const key = await crypto.subtle.importKey('raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveBits']);
  const bits = await crypto.subtle.deriveBits({ name: 'PBKDF2', hash: 'SHA-256', salt, iterations: 100000 }, key, 256);
  return new Uint8Array(bits);
}
function timingSafeEqual(a, b) { if (a.length !== b.length) return false; let value = 0; for (let i = 0; i < a.length; i++) value |= a[i] ^ b[i]; return value === 0; }
function bearer(request) { const value = request.headers.get('authorization') || ''; return value.toLowerCase().startsWith('bearer ') ? value.slice(7).trim() : null; }
function device(request, body) { return String(request.headers.get('x-device-id') || body.deviceId || '').trim().slice(0, 128); }
async function safeBody(request) { try { return await request.json(); } catch { return {}; } }
function id(prefix) { return `${prefix}-${crypto.randomUUID().replaceAll('-', '').toUpperCase()}`; }
function hex(bytes) { return [...bytes].map(x => x.toString(16).padStart(2, '0')).join('').toUpperCase(); }
function toBase64(bytes) { return btoa(String.fromCharCode(...bytes)); }
function fromBase64(value) { return Uint8Array.from(atob(value), c => c.charCodeAt(0)); }
