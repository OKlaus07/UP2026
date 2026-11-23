-- Выполняйте после подключения DBeaver к базе club_registration.
CREATE TABLE users(id SERIAL PRIMARY KEY, first_name VARCHAR(80) NOT NULL, last_name VARCHAR(80) NOT NULL, middle_name VARCHAR(80), email VARCHAR(254) NOT NULL UNIQUE, phone VARCHAR(30), password_hash VARCHAR(100) NOT NULL, password_salt BYTEA NOT NULL, role VARCHAR(10) NOT NULL DEFAULT 'user' CHECK(role IN ('user','admin')), account_category VARCHAR(30) NOT NULL DEFAULT 'student' CHECK(account_category IN ('parent','student','college_student','teacher')), is_active BOOLEAN NOT NULL DEFAULT TRUE, created_at TIMESTAMPTZ NOT NULL DEFAULT now());
CREATE TABLE categories(id SERIAL PRIMARY KEY, name VARCHAR(100) NOT NULL UNIQUE);
CREATE TABLE teachers(id SERIAL PRIMARY KEY, first_name VARCHAR(80) NOT NULL, last_name VARCHAR(80) NOT NULL, middle_name VARCHAR(80), specialization VARCHAR(150) NOT NULL, phone VARCHAR(30), description TEXT, is_active BOOLEAN NOT NULL DEFAULT TRUE, user_id INT REFERENCES users(id) ON DELETE SET NULL);
CREATE TABLE clubs(id SERIAL PRIMARY KEY, name VARCHAR(150) NOT NULL, category_id INT NOT NULL REFERENCES categories(id), description TEXT NOT NULL DEFAULT '', min_age INT NOT NULL DEFAULT 5 CHECK(min_age>=0), max_age INT NOT NULL DEFAULT 18 CHECK(max_age>=min_age), price NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK(price>=0), capacity INT NOT NULL CHECK(capacity>0), teacher_id INT REFERENCES teachers(id), is_active BOOLEAN NOT NULL DEFAULT TRUE);
CREATE TABLE schedules(id SERIAL PRIMARY KEY, club_id INT NOT NULL REFERENCES clubs(id) ON DELETE CASCADE, teacher_id INT NOT NULL REFERENCES teachers(id), weekday INT NOT NULL CHECK(weekday BETWEEN 1 AND 7), start_time TIME NOT NULL, end_time TIME NOT NULL CHECK(end_time>start_time), room VARCHAR(80) NOT NULL);
-- Проверка пересечений также выполняется в самой БД, чтобы защититься от параллельных изменений.
CREATE OR REPLACE FUNCTION check_schedule_overlap() RETURNS trigger AS $$
BEGIN
  PERFORM pg_advisory_xact_lock(NEW.weekday);
  IF EXISTS (SELECT 1 FROM schedules s WHERE s.weekday=NEW.weekday AND s.id<>COALESCE(NEW.id,-1)
    AND s.start_time<NEW.end_time AND s.end_time>NEW.start_time
    AND (s.teacher_id=NEW.teacher_id OR lower(s.room)=lower(NEW.room))) THEN
    RAISE EXCEPTION 'Преподаватель или кабинет уже занят в это время';
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;
CREATE TRIGGER schedules_no_overlap BEFORE INSERT OR UPDATE ON schedules FOR EACH ROW EXECUTE PROCEDURE check_schedule_overlap();
CREATE TABLE enrollments(id SERIAL PRIMARY KEY, user_id INT NOT NULL REFERENCES users(id), club_id INT NOT NULL REFERENCES clubs(id), created_at TIMESTAMPTZ NOT NULL DEFAULT now(), status VARCHAR(12) NOT NULL DEFAULT 'pending' CHECK(status IN ('pending','approved','rejected','cancelled')));
CREATE INDEX ix_clubs_category ON clubs(category_id); CREATE INDEX ix_enrollments_user ON enrollments(user_id); CREATE INDEX ix_enrollments_club_status ON enrollments(club_id,status); CREATE INDEX ix_schedules_club_day ON schedules(club_id,weekday);
CREATE UNIQUE INDEX ux_enrollment_active ON enrollments(user_id,club_id) WHERE status IN ('pending','approved');
INSERT INTO categories(name) VALUES ('Технологии'),('Искусство'),('Спорт'),('Языки'),('Музыка');
INSERT INTO teachers(first_name,last_name,specialization) VALUES ('Анна','Иванова','Программирование'),('Илья','Петров','Робототехника'),('Мария','Соколова','Рисование'),('Олег','Кузнецов','Футбол');
INSERT INTO clubs(name,category_id,description,min_age,max_age,price,capacity,teacher_id) VALUES
('Программирование',1,'Основы алгоритмов и создание первых программ.',10,17,1500,12,1),('Робототехника',1,'Конструирование и программирование роботов.',9,16,1800,10,2),('Рисование',2,'Знакомство с цветом, композицией и разными материалами.',6,16,900,14,3),('Футбол',3,'Тренировки для начинающих и продолжающих.',8,16,1200,18,4),('Шахматы',3,'Логика, тактика и турнирная практика.',7,17,700,12,1),('Английский язык',4,'Разговорная практика и основы грамматики.',8,16,1000,14,3),('Вокал',5,'Постановка голоса и ансамблевое пение.',9,17,1100,10,3);
INSERT INTO schedules(club_id,teacher_id,weekday,start_time,end_time,room) VALUES (1,1,2,'16:00','17:00','Кабинет 12'),(2,2,3,'17:00','18:30','Лаборатория 4'),(3,3,4,'15:30','16:30','Студия 2'),(4,4,6,'16:00','17:30','Спортзал'),(5,1,5,'16:00','17:00','Кабинет 12'),(6,3,2,'17:00','18:00','Кабинет 8'),(7,3,6,'15:00','16:00','Музыкальный класс');
-- Первый администратор создаётся безопасно: зарегистрируйтесь через окно приложения,
-- затем от имени владельца БД измените роль только своей учётной записи:
-- UPDATE users SET role='admin' WHERE email='oklaus@mail.ru';
