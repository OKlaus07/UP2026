-- Безопасное расширение существующей базы club_registration.
-- Скрипт не удаляет и не перезаписывает пользовательские данные.
-- Выполните его, подключившись к базе club_registration. Повторный запуск безопасен.

CREATE TABLE IF NOT EXISTS application_settings (
    setting_key VARCHAR(100) PRIMARY KEY,
    setting_value TEXT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS user_preferences (
    user_id INTEGER PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    preferred_theme VARCHAR(10) NOT NULL DEFAULT 'light' CHECK (preferred_theme IN ('light','dark')),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS audit_log (
    id BIGSERIAL PRIMARY KEY,
    actor_user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(80) NOT NULL,
    entity_id INTEGER,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS notifications (
    id BIGSERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    message TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

INSERT INTO application_settings(setting_key, setting_value)
VALUES ('christmas_theme_enabled', 'true')
ON CONFLICT (setting_key) DO NOTHING;

CREATE INDEX IF NOT EXISTS ix_audit_log_created_at ON audit_log(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_user_unread ON notifications(user_id, is_read, created_at DESC);
