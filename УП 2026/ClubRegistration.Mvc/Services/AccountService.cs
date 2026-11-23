using ClubRegistration.Mvc.Models;
using Npgsql;
using System;
using System.Security.Cryptography;

namespace ClubRegistration.Mvc.Services
{
    // Регистрация обычных пользователей и проверка пароля при входе.
    public class AccountService
    {
        // Повторно проверяет активность и роль из БД для действующей cookie-сессии.
        public bool IsAccountAllowed(int userId, string cookieRole)
        {
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand("SELECT is_active,role FROM users WHERE id=@id", connection))
            {
                command.Parameters.AddWithValue("id", userId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() && reader.GetBoolean(0) && reader.GetString(1) == cookieRole;
                }
            }
        }

        // В БД попадает только PBKDF2-хеш пароля и его индивидуальная соль.
        public string Register(RegisterModel model)
        {
            if (model == null || (model.AccountCategory != "parent" && model.AccountCategory != "student" && model.AccountCategory != "college_student" && model.AccountCategory != "teacher"))
                return "Выберите корректную категорию аккаунта.";
            byte[] salt = PasswordService.NewSalt();
            string hash = PasswordService.Hash(model.Password, salt);
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand("INSERT INTO users(first_name,last_name,middle_name,email,phone,password_hash,password_salt,role,account_category) VALUES(@first,@last,@middle,@email,@phone,@hash,@salt,'user',@category)", connection))
            {
                command.Parameters.AddWithValue("first", model.FirstName.Trim());
                command.Parameters.AddWithValue("last", model.LastName.Trim());
                command.Parameters.AddWithValue("middle", string.IsNullOrWhiteSpace(model.MiddleName) ? (object)DBNull.Value : model.MiddleName.Trim());
                command.Parameters.AddWithValue("email", model.Email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("phone", model.Phone.Trim());
                command.Parameters.AddWithValue("hash", hash);
                command.Parameters.AddWithValue("salt", salt);
                command.Parameters.AddWithValue("category", model.AccountCategory);
                try { command.ExecuteNonQuery(); return null; }
                catch (PostgresException error) when (error.SqlState == "23505") { return "Пользователь с такой почтой уже зарегистрирован."; }
            }
        }

        public int? Login(string email, string password, out string role)
        {
            string accountCategory;
            return Login(email, password, out role, out accountCategory);
        }

        public int? Login(string email, string password, out string role, out string accountCategory)
        {
            role = null;
            accountCategory = null;
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand("SELECT id,password_hash,password_salt,role,is_active,account_category FROM users WHERE email=@email", connection))
            {
                command.Parameters.AddWithValue("email", (email ?? "").Trim().ToLowerInvariant());
                int id;
                string savedHash;
                byte[] salt;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read() || !reader.GetBoolean(4)) return null;
                    id = reader.GetInt32(0);
                    savedHash = reader.GetString(1);
                    salt = (byte[])reader[2];
                    role = reader.GetString(3);
                    accountCategory = reader.GetString(5);
                }

                if (!PasswordService.Verify(password ?? "", salt, savedHash)) return null;

                // При успешном входе незаметно обновляем хеш старого формата.
                if (PasswordService.IsLegacyHash(password, salt, savedHash))
                {
                    byte[] newSalt = PasswordService.NewSalt();
                    using (var upgrade = new NpgsqlCommand("UPDATE users SET password_hash=@hash,password_salt=@salt WHERE id=@id", connection))
                    {
                        upgrade.Parameters.AddWithValue("hash", PasswordService.Hash(password, newSalt));
                        upgrade.Parameters.AddWithValue("salt", newSalt);
                        upgrade.Parameters.AddWithValue("id", id);
                        upgrade.ExecuteNonQuery();
                    }
                }
                return id;
            }
        }

    }
}
