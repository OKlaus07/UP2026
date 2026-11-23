-- Безопасная миграция категорий профиля для существующей базы.
-- В DBeaver откройте подключение club_registration, выберите SQL Editor,
-- откройте этот файл и выполните его как скрипт (Alt+X).
-- Не удаляет пользователей и сохраняет все текущие данные.
ALTER TABLE users
    ADD COLUMN IF NOT EXISTS account_category VARCHAR(30) NOT NULL DEFAULT 'student';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_users_account_category'
          AND conrelid = 'users'::regclass
    ) THEN
        ALTER TABLE users
            ADD CONSTRAINT ck_users_account_category
            CHECK (account_category IN ('parent','student','college_student','teacher'));
    END IF;
END $$;

COMMENT ON COLUMN users.account_category IS 'Тип профиля: parent, student, college_student или teacher';
