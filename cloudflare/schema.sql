CREATE TABLE IF NOT EXISTS users (
  application_user_id TEXT PRIMARY KEY,
  player_id TEXT NOT NULL,
  username TEXT NOT NULL UNIQUE COLLATE NOCASE,
  password_salt TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS sessions (
  token TEXT PRIMARY KEY,
  application_user_id TEXT NOT NULL,
  expires_at TEXT NOT NULL,
  FOREIGN KEY(application_user_id) REFERENCES users(application_user_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teams (
  team_id TEXT PRIMARY KEY,
  application_user_id TEXT NOT NULL,
  name TEXT NOT NULL,
  created_at TEXT NOT NULL,
  FOREIGN KEY(application_user_id) REFERENCES users(application_user_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_teams_user ON teams(application_user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_expiry ON sessions(expires_at);
