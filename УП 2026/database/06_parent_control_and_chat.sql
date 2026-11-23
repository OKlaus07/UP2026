-- Дополнение существующей базы: родительский контроль и личные чаты.
-- Подключитесь в DBeaver к club_registration и выполните файл как SQL script.
-- Скрипт идемпотентный и не удаляет существующие учётные записи или заявки.

-- Аккаунт преподавателя связывается с карточкой преподавателя администратором.
ALTER TABLE teachers
    ADD COLUMN IF NOT EXISTS user_id INTEGER REFERENCES users(id) ON DELETE SET NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_teachers_user_id
    ON teachers(user_id) WHERE user_id IS NOT NULL;

CREATE OR REPLACE FUNCTION check_teacher_account_category() RETURNS trigger AS $$
DECLARE selected_category VARCHAR(30);
BEGIN
    IF NEW.user_id IS NOT NULL THEN
        SELECT account_category INTO selected_category FROM users WHERE id=NEW.user_id AND is_active=true;
        IF selected_category IS DISTINCT FROM 'teacher' THEN
            RAISE EXCEPTION 'К карточке преподавателя можно привязать только активный аккаунт категории «Преподаватель»';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS teacher_account_category_check ON teachers;
CREATE TRIGGER teacher_account_category_check BEFORE INSERT OR UPDATE OF user_id ON teachers
    FOR EACH ROW EXECUTE PROCEDURE check_teacher_account_category();

-- Родитель сначала отправляет приглашение, ученик подтверждает привязку из своего аккаунта.
CREATE TABLE IF NOT EXISTS parent_student_links (
    id BIGSERIAL PRIMARY KEY,
    parent_user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    student_user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status VARCHAR(12) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','active','rejected')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    decided_at TIMESTAMPTZ,
    CHECK (parent_user_id <> student_user_id),
    UNIQUE (parent_user_id, student_user_id)
);
CREATE INDEX IF NOT EXISTS ix_parent_student_parent ON parent_student_links(parent_user_id,status);
CREATE INDEX IF NOT EXISTS ix_parent_student_student ON parent_student_links(student_user_id,status);

CREATE OR REPLACE FUNCTION check_parent_student_account_categories() RETURNS trigger AS $$
DECLARE parent_category VARCHAR(30);
DECLARE student_category VARCHAR(30);
BEGIN
    SELECT account_category INTO parent_category FROM users WHERE id=NEW.parent_user_id;
    SELECT account_category INTO student_category FROM users WHERE id=NEW.student_user_id;
    IF parent_category IS DISTINCT FROM 'parent' OR student_category IS DISTINCT FROM 'student' THEN
        RAISE EXCEPTION 'Связь может объединять только аккаунт родителя и аккаунт ученика';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS parent_student_category_check ON parent_student_links;
CREATE TRIGGER parent_student_category_check BEFORE INSERT OR UPDATE ON parent_student_links
    FOR EACH ROW EXECUTE PROCEDURE check_parent_student_account_categories();

CREATE OR REPLACE FUNCTION protect_linked_account_categories() RETURNS trigger AS $$
BEGIN
    IF NEW.account_category IS DISTINCT FROM OLD.account_category AND (
        (NEW.account_category <> 'parent' AND EXISTS(SELECT 1 FROM parent_student_links WHERE parent_user_id=NEW.id AND status IN ('pending','active'))) OR
        (NEW.account_category <> 'student' AND EXISTS(SELECT 1 FROM parent_student_links WHERE student_user_id=NEW.id AND status IN ('pending','active'))) OR
        (NEW.account_category <> 'teacher' AND EXISTS(SELECT 1 FROM teachers WHERE user_id=NEW.id))
    ) THEN
        RAISE EXCEPTION 'Сначала удалите семейные связи или связь с карточкой преподавателя';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS linked_account_category_check ON users;
CREATE TRIGGER linked_account_category_check BEFORE UPDATE OF account_category ON users
    FOR EACH ROW EXECUTE PROCEDURE protect_linked_account_categories();

-- Чат создаётся для конкретной пары ученик-преподаватель только при наличии заявки ученика.
CREATE TABLE IF NOT EXISTS chat_conversations (
    id BIGSERIAL PRIMARY KEY,
    student_user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    teacher_id INTEGER NOT NULL REFERENCES teachers(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (student_user_id, teacher_id)
);
CREATE TABLE IF NOT EXISTS chat_messages (
    id BIGSERIAL PRIMARY KEY,
    conversation_id BIGINT NOT NULL REFERENCES chat_conversations(id) ON DELETE CASCADE,
    sender_user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    body VARCHAR(4000) NOT NULL CHECK (length(trim(body)) BETWEEN 1 AND 4000),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_chat_messages_conversation ON chat_messages(conversation_id,created_at,id);

COMMENT ON TABLE parent_student_links IS 'Связь родителя с учеником; приглашение подтверждает ученик.';
COMMENT ON TABLE chat_conversations IS 'Диалоги родителя/ученика с преподавателем кружка.';
