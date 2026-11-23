// ============================================================
// PerformanceBenchmarks.cs
// Практическая часть задания от 25.11.2026:
// «Оценка эффективности оптимизаций» (пункт 4).
//
// Класс сравнивает три способа обработки списка кружков:
//   1) Наивный поиск   — перебор всего списка (O(n));
//   2) Кэш-словарь     — Dictionary с готовым результатом (O(1));
//   3) Многопоточность — Task.WhenAll делит работу на части.
//
// Каждый замер оборачивается в PerformanceMetrics.Time(...),
// поэтому результаты видны и на странице /Diagnostics,
// и в логах (задание от 24.11.2026 — логирование).
//
// Модуль не обращается к PostgreSQL и не меняет поведение
// существующих страниц — его можно запускать безопасно.
// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClubRegistration.Mvc.Services.Optimization
{
    public class PerformanceBenchmarks
    {
        // Логгер проекта подключён через appsettings.json;
        // здесь используем стандартный ILogger системы Microsoft.
        private readonly Microsoft.Extensions.Logging.ILogger<PerformanceBenchmarks> _logger;

        // Конструктор с внедрением зависимостей (DI): ASP.NET Core
        // сам передаст логгер, когда мы зарегистрируем класс в Startup.
        public PerformanceBenchmarks(Microsoft.Extensions.Logging.ILogger<PerformanceBenchmarks> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Полная демонстрация сравнения методов оптимизации.
        /// Возвращает человекочитаемый отчёт для страницы диагностики.
        /// </summary>
        public string RunFullComparison(int itemCount = 50000)
        {
            // Генерируем тестовые названия кружков.
            var clubs = GenerateClubNames(itemCount);

            // ---------- Замер 1: наивный линейный поиск ----------
            // Имитирует частый запрос "найти кружок по названию".
            long naiveHits = PerformanceMetrics.Time("Benchmark.NaiveSearch", () =>
            {
                long found = 0;
                for (int i = 0; i < 200; i++)
                {
                    string target = clubs[(i * 977) % clubs.Count];
                    if (clubs.Any(name => name == target)) found++;
                }
                return found;
            });

            // ---------- Замер 2: оптимизация структурой данных ----------
            // Словарь Dictionary даёт O(1) вместо O(n).
            long cachedHits = PerformanceMetrics.Time("Benchmark.DictionaryLookup", () =>
            {
                var index = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < clubs.Count; i++) index[clubs[i]] = i;

                long found = 0;
                for (int i = 0; i < 200; i++)
                {
                    string target = clubs[(i * 977) % clubs.Count];
                    int position;
                    if (index.TryGetValue(target, out position)) found++;
                }
                return found;
            });

            // ---------- Замер 3: многопоточность (TPL) ----------
            // Делим подсчёт слов на 4 задачи и запускаем параллельно.
            long parallelResult = PerformanceMetrics.Time("Benchmark.ParallelProcessing", () =>
            {
                int degreeOfParallelism = Math.Max(2, Environment.ProcessorCount);
                var chunkSize = (int)Math.Ceiling(clubs.Count / (double)degreeOfParallelism);

                var tasks = new List<Task<long>>();
                for (int start = 0; start < clubs.Count; start += chunkSize)
                {
                    int from = start;
                    int to = Math.Min(start + chunkSize, clubs.Count);
                    tasks.Add(Task.Run(() => CountLongNames(clubs, from, to)));
                }

                // Wait() дожидаемся всех задач и суммируем результаты.
                return Task.WhenAll(tasks).ContinueWith(t => t.Result.Sum()).Result;
            });

            // ---------- Отчёт ----------
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Элементов в наборе: {itemCount}");
            report.AppendLine($"Найденных совпадений: наивный={naiveHits}, кэш={cachedHits}, параллельно={parallelResult}");
            report.AppendLine();
            report.AppendLine("Сводка метрик (мс):");
            foreach (var m in PerformanceMetrics.Snapshot().Where(x => x.Name.StartsWith("Benchmark.")))
            {
                report.AppendLine(
                    $"  {m.Name}: вызовов={m.Count}, всего={m.TotalMs:F2}, среднее={m.AvgMs:F2}, макс={m.MaxMs:F2}");
            }

            // Логируем результат — см. задание от 24.11.2026 про логирование.
            _logger.LogInformation("Бенчмарк производительности выполнен: элементов={Count}", itemCount);

            return report.ToString();
        }

        // Вспомогательные методы

        private static List<string> GenerateClubNames(int count)
        {
            // Список названий без дубликатов; порядок случайный,
            // чтобы алгоритмы сортировки работали на «неотсортированных» данных.
            var random = new Random(2026); // фиксированное зерно — воспроизводимость замеров
            var names = new List<string>(count);
            for (int i = 0; i < count; i++)
                names.Add("Кружок-" + random.Next(1_000_000).ToString("D6") + "-" + i);
            return names;
        }

        private static long CountLongNames(IReadOnlyList<string> items, int from, int to)
        {
            long result = 0;
            for (int i = from; i < to; i++)
                if (items[i].Length > 18) result++;
            return result;
        }
    }
}
