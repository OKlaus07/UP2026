
// ============================================================
// Db.cs
// Централизованное управление подключением PostgreSQL
// ============================================================

using System;

using Npgsql;

namespace ClubRegistration.Mvc.Services
{
    public static class Db
    {
        // Строка подключения загружается из конфигурации приложения.
        private static string _connectionString;

        // ----------------------------------------------------
        // Configure
        // Сохранение строки подключения при запуске приложения
        // ----------------------------------------------------
        public static void Configure(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Не задан ConnectionStrings:DefaultConnection. Настройте User Secrets."
                );
            }

            _connectionString = connectionString;
        }

        // ----------------------------------------------------
        // Open
        // Создание и открытие подключения к PostgreSQL
        // ----------------------------------------------------
        public static NpgsqlConnection Open()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "Подключение PostgreSQL не настроено."
                );
            }

            var connection = new NpgsqlConnection(_connectionString);

            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }
}
