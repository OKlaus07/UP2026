-- Сначала зарегистрируйте администратора через приложение.
-- Выполните этот запрос в DBeaver, подключившись владельцем базы.
-- Замените адрес на свой. Обычная регистрационная форма всегда задаёт роль user.
UPDATE users SET role='admin' WHERE email='ваш_email@example.com';
-- Убедитесь, что изменена ровно нужная запись:
SELECT id, first_name, last_name, email, role FROM users WHERE email='ваш_email@example.com';
