CREATE TABLE IF NOT EXISTS preview_users (
  application_user_id TEXT PRIMARY KEY,
  player_id TEXT NOT NULL UNIQUE,
  username TEXT NOT NULL,
  username_norm TEXT NOT NULL UNIQUE,
  password_salt TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS preview_sessions (
  token TEXT PRIMARY KEY,
  application_user_id TEXT NOT NULL,
  expires_at TEXT NOT NULL,
  created_at TEXT NOT NULL,
  FOREIGN KEY (application_user_id) REFERENCES preview_users(application_user_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_preview_sessions_user ON preview_sessions(application_user_id);
CREATE INDEX IF NOT EXISTS ix_preview_sessions_expiry ON preview_sessions(expires_at);

CREATE TABLE IF NOT EXISTS preview_teams (
  team_id TEXT PRIMARY KEY,
  application_user_id TEXT NOT NULL,
  name TEXT NOT NULL,
  created_at TEXT NOT NULL,
  FOREIGN KEY (application_user_id) REFERENCES preview_users(application_user_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_preview_teams_user ON preview_teams(application_user_id);
