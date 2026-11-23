using Npgsql;
using System;

namespace ClubRegistration.Mvc.Services
{
    // Параметры сайта хранятся в PostgreSQL, чтобы настройки переживали перезапуск.
    public class SiteSettingsService
    {
        public string GetUserTheme(int? userId, string cookieTheme)
        {
            if (userId.HasValue)
            {
                try
                {
                    using var connection = Db.Open();
                    using var command = new NpgsqlCommand("SELECT preferred_theme FROM user_preferences WHERE user_id=@user", connection);
                    command.Parameters.AddWithValue("user", userId.Value);
                    object value = command.ExecuteScalar();
                    if (value != null && value != DBNull.Value) return Convert.ToString(value);
                }
                catch { /* До запуска миграции используем настройку cookie. */ }
            }
            return cookieTheme == "dark" ? "dark" : "light";
        }

        public void SaveUserTheme(int userId, string theme)
        {
            using var connection = Db.Open();
            using var command = new NpgsqlCommand(@"INSERT INTO user_preferences(user_id,preferred_theme,updated_at)
                VALUES(@user,@theme,now()) ON CONFLICT(user_id) DO UPDATE SET preferred_theme=EXCLUDED.preferred_theme,updated_at=now()", connection);
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("theme", theme);
            command.ExecuteNonQuery();
        }

        public bool IsChristmasThemeEnabled()
        {
            try
            {
                using var connection = Db.Open();
                using var command = new NpgsqlCommand("SELECT setting_value FROM application_settings WHERE setting_key='christmas_theme_enabled'", connection);
                object value = command.ExecuteScalar();
                return value == null || value == DBNull.Value || Convert.ToBoolean(value);
            }
            catch { return true; }
        }

        public void SetChristmasTheme(bool enabled, int adminId)
        {
            using var connection = Db.Open();
            using var transaction = connection.BeginTransaction();
            using (var command = new NpgsqlCommand(@"INSERT INTO application_settings(setting_key,setting_value,updated_at)
                VALUES('christmas_theme_enabled',@value,now())
                ON CONFLICT(setting_key) DO UPDATE SET setting_value=EXCLUDED.setting_value,updated_at=now()", connection, transaction))
            {
                command.Parameters.AddWithValue("value", enabled ? "true" : "false");
                command.ExecuteNonQuery();
            }
            using (var command = new NpgsqlCommand("INSERT INTO audit_log(actor_user_id,action,entity_type,details) VALUES(@actor,'theme_changed','application_setting',@details)", connection, transaction))
            {
                command.Parameters.AddWithValue("actor", adminId);
                command.Parameters.AddWithValue("details", "Новогодняя тема: " + (enabled ? "включена" : "выключена"));
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }
}
