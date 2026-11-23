// ============================================================
// PerformanceMetrics.cs
// Практическая часть задания от 25.11.2026:
// «Определение возможности увеличения быстродействия продукта».
//
// Модуль собирает простейшие метрики производительности:
//  - сколько раз вызывался участок кода;
//  - суммарное, среднее, минимальное и максимальное время;
//  - количество ошибок внутри замеряемых участков.
//
// Замеры выполняются через System.Diagnostics.Stopwatch —
// стандартный высокоточный таймер .NET (тот же инструмент,
// что разбирается в теоретической части работы).
//
// ВАЖНО: модуль полностью автономный — он не трогает базу
// данных и существующие сервисы, поэтому добавление этого
// файла ничего не ломает в приложении.
// ============================================================

using System;
using System.Collections.Concurrent;   // потокобезопасный словарь
using System.Diagnostics;              // Stopwatch для замеров
using System.Linq;                     // LINQ для сводной таблицы
using System.Threading;                // Interlocked — атомарные операции

namespace ClubRegistration.Mvc.Services.Optimization
{
    /// <summary>
    /// Хранилище метрик производительности. Класс статический,
    /// потому что метрики нужны всему приложению целиком
    /// (сервисам, контроллерам и странице диагностики).
    /// </summary>
    public static class PerformanceMetrics
    {
        // Потокобезопасный словарь: ASP.NET Core обрабатывает
        // запросы в нескольких потоках одновременно, поэтому
        // обычная Dictionary здесь дала бы «состояние гонки».
        private static readonly ConcurrentDictionary<string, MetricData> _metrics =
            new ConcurrentDictionary<string, MetricData>();

        // Ограничение на число разных метрик, чтобы словарь
        // не рос бесконечно при ошибке в коде (защита памяти).
        private const int MaxMetrics = 500;

        /// <summary>
        /// Выполняет действие и засекает время его работы.
        /// Используется как обёртка над уже существующим кодом,
        /// поэтому вызывать её можно из любого места без риска
        /// изменить логику приложения.
        /// </summary>
        /// <param name="name">Имя участка кода, например "ClubService.Search".</param>
        /// <param name="action">Измеряемое действие.</param>
        public static void Time(string name, Action action)
        {
            var data = GetOrAdd(name);

            // StartNew() запускает таймер одной командой —
            // так в замер попадает меньше лишнего времени.
            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();                       // выполняется исходный код
            }
            catch
            {
                // Ошибку считаем, но НЕ глушим — пробрасываем дальше,
                // чтобы поведение приложения не изменилось.
                Interlocked.Increment(ref data.Errors);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                Record(data, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Выполняет функцию с результатом и засекает время.
        /// Возвращает результат исходной функции без изменений.
        /// </summary>
        public static T Time<T>(string name, Func<T> function)
        {
            var data = GetOrAdd(name);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return function();
            }
            catch
            {
                Interlocked.Increment(ref data.Errors);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                Record(data, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>Возвращает снимок всех накопленных метрик.</summary>
        public static System.Collections.Generic.List<MetricSnapshot> Snapshot()
        {
            // Копируем данные, чтобы страница диагностики
            // не увидела «половину» обновлённой цифры.
            return _metrics.Select(pair => pair.Value.Clone(pair.Key))
                           .OrderByDescending(m => m.TotalMs)
                           .ToList();
        }

        /// <summary>Обнуляет метрики (например, перед новым прогоном замеров).</summary>
        public static void Reset()
        {
            foreach (var data in _metrics.Values) data.Reset();
        }

        // ---------- служебные методы ----------

        private static MetricData GetOrAdd(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя метрики не может быть пустым.", nameof(name));

            // Если метрик уже слишком много, возвращаем «заглушку»,
            // чтобы не съедать память: неограниченный рост словаря —
            // типичная ошибка проектирования систем мониторинга.
            if (_metrics.Count >= MaxMetrics && !_metrics.ContainsKey(name))
                return new MetricData();

            return _metrics.GetOrAdd(name, _ => new MetricData());
        }

        private static void Record(MetricData data, double elapsedMs)
        {
            // Interlocked используется вместо lock: это быстрее
            // и исключает взаимоблокировки на «горячем» пути.
            Interlocked.Increment(ref data.Count);
            AddToTotal(data, elapsedMs);
            UpdateMinMax(data, elapsedMs);
        }

        private static void AddToTotal(MetricData data, double elapsedMs)
        {
            // Атомарное сложение дробных чисел через CAS-цикл:
            // несколько потоков не потеряют свои замеры.
            while (true)
            {
                long currentRaw = Interlocked.Read(ref data._totalMsBits);
                double current = BitConverter.Int64BitsToDouble(currentRaw);
                long newValue = BitConverter.DoubleToInt64Bits(current + elapsedMs);
                if (Interlocked.CompareExchange(ref data._totalMsBits, newValue, currentRaw) == currentRaw)
                    return;
            }
        }

        private static void UpdateMinMax(MetricData data, double elapsedMs)
        {
            // Простейший CAS-цикл для обновления минимума...
            while (true)
            {
                long minRaw = Interlocked.Read(ref data._minMsBits);
                double min = BitConverter.Int64BitsToDouble(minRaw);
                if (elapsedMs >= min) break;
                if (Interlocked.CompareExchange(
                        ref data._minMsBits, BitConverter.DoubleToInt64Bits(elapsedMs), minRaw) == minRaw)
                    break;
            }

            // ...и для максимума.
            while (true)
            {
                long maxRaw = Interlocked.Read(ref data._maxMsBits);
                double max = BitConverter.Int64BitsToDouble(maxRaw);
                if (elapsedMs <= max) break;
                if (Interlocked.CompareExchange(
                        ref data._maxMsBits, BitConverter.DoubleToInt64Bits(elapsedMs), maxRaw) == maxRaw)
                    break;
            }
        }

        /// <summary>
        /// Внутренняя изменяемая запись одной метрики.
        /// Числа с плавающей точкой обновляются атомарно через
        /// Interlocked и хранение «сырых» битов double в long.
        /// </summary>
        private class MetricData
        {
            public long Count;           // число вызовов
            public long Errors;          // число исключений
            internal long _totalMsBits;  // double в битовом виде (для CAS)
            internal long _minMsBits = BitConverter.DoubleToInt64Bits(double.MaxValue);
            internal long _maxMsBits;    // 0 — максимум ещё не записан

            public void Reset()
            {
                Count = 0;
                Errors = 0;
                Interlocked.Exchange(ref _totalMsBits, 0);
                Interlocked.Exchange(ref _minMsBits, BitConverter.DoubleToInt64Bits(double.MaxValue));
                Interlocked.Exchange(ref _maxMsBits, 0);
            }

            public MetricSnapshot Clone(string name)
            {
                double total = BitConverter.Int64BitsToDouble(Interlocked.Read(ref _totalMsBits));
                double min = BitConverter.Int64BitsToDouble(Interlocked.Read(ref _minMsBits));
                double max = BitConverter.Int64BitsToDouble(Interlocked.Read(ref _maxMsBits));

                return new MetricSnapshot
                {
                    Name = name,
                    Count = Count,
                    Errors = Errors,
                    TotalMs = total,
                    AvgMs = Count > 0 ? total / Count : 0,
                    MinMs = Count > 0 ? min : 0,   // если замеров нет — показываем 0, а не MaxValue
                    MaxMs = max
                };
            }
        }
    }

    /// <summary>
    /// Неизменяемый снимок метрики — то, что отдаётся на страницу
    /// диагностики и в модульные тесты.
    /// </summary>
    public class MetricSnapshot
    {
        public string Name { get; set; }     // имя участка кода
        public long Count { get; set; }      // сколько раз выполнялся
        public long Errors { get; set; }     // сколько раз упал с исключением
        public double TotalMs { get; set; }  // суммарное время, мс
        public double AvgMs { get; set; }    // среднее время, мс
        public double MinMs { get; set; }    // лучший замер, мс
        public double MaxMs { get; set; }    // худший замер, мс
    }
}
