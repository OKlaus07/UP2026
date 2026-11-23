// ============================================================
// OptimizationModuleTests.cs
// Модульные тесты для модуля оптимизации (задания 24.11 и 25.11.2026).
//
// Тесты НЕ требуют PostgreSQL и ASP.NET Core — они проверяют
// чистую логику классов из Services/Optimization, поэтому
// проходят в любом окружении вместе с существующими тестами xUnit.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using ClubRegistration.Mvc.Services.Optimization;
using Xunit;

namespace ClubRegistration.Tests
{
    public class OptimizationModuleTests
    {
        // ---------- SortingAlgorithms: корректность сортировок ----------

        [Fact]
        public void BubbleSort_SortsNamesAlphabetically()
        {
            var items = new List<string> { "Танцы", "Артём", "Шахматы", "Дзюдо" };
            SortingAlgorithms.BubbleSort(items);
            Assert.Equal(new[] { "Артём", "Дзюдо", "Танцы", "Шахматы" }, items);
        }

        [Fact]
        public void QuickSort_ProducesSameResultAsBubbleSort()
        {
            // Ключевая проверка модификации (задание 24.11):
            // новый алгоритм обязан давать тот же результат, что и старый.
            var random = new Random(42); // фиксированное зерно — тест воспроизводим
            var source = new List<string>();
            for (int i = 0; i < 300; i++)
                source.Add("Кружок-" + random.Next(1000));

            var byBubble = new List<string>(source);
            var byQuick = new List<string>(source);

            SortingAlgorithms.BubbleSort(byBubble);
            SortingAlgorithms.QuickSort(byQuick);

            Assert.Equal(byBubble, byQuick);
        }

        [Fact]
        public void QuickSort_HandlesAlreadySortedData()
        {
            // Почти отсортированные данные — классический случай,
            // где «плохой» QuickSort деградирует. Медиана из трёх
            // должна это выдержать.
            var items = Enumerable.Range(0, 500).Select(i => "A" + i.ToString("D4")).ToList();
            SortingAlgorithms.QuickSort(items);
            Assert.True(items.SequenceEqual(items.OrderBy(x => x, StringComparer.Ordinal)));
        }

        [Fact]
        public void QuickSort_RejectsNull()
        {
            // Проверка входных данных — один из пунктов задания 24.11.
            Assert.Throws<ArgumentNullException>(() => SortingAlgorithms.QuickSort(null));
        }

        // ---------- SortedClubIndex: структура данных с кэшем ----------

        [Fact]
        public void SortedClubIndex_IgnoresDuplicatesAndCase()
        {
            var index = new SortingAlgorithms.SortedClubIndex();
            index.Add("Робототехника");
            index.Add("робототехника"); // дубликат без учёта регистра

            Assert.Equal(1, index.Count);
            Assert.True(index.Contains("РОБОТОТЕХНИКА"));
        }

        [Fact]
        public void SortedClubIndex_ReturnsSortedCachedSnapshot()
        {
            var index = new SortingAlgorithms.SortedClubIndex();
            index.Add("Бисероплетение");
            index.Add("Акробатика");

            var first = index.GetSorted();
            var second = index.GetSorted(); // повторный вызов берётся из кэша

            Assert.Equal(new[] { "Акробатика", "Бисероплетение" }, first.ToArray());
            Assert.Same(first, second); // массив не пересобран — кэш работает
        }

        [Fact]
        public void SortedClubIndex_RejectsEmptyName()
        {
            var index = new SortingAlgorithms.SortedClubIndex();
            Assert.Throws<ArgumentException>(() => index.Add("   "));
        }

        // ---------- PerformanceMetrics: сбор замеров ----------

        [Fact]
        public void Metrics_Time_RecordsOneCall()
        {
            const string name = "Test.Metrics.Time";
            PerformanceMetrics.Reset();

            PerformanceMetrics.Time(name, () => { /* имитация работы */ });

            var snapshot = PerformanceMetrics.Snapshot().Single(m => m.Name == name);
            Assert.Equal(1, snapshot.Count);
            Assert.Equal(0, snapshot.Errors);
            Assert.True(snapshot.TotalMs >= 0);
        }

        [Fact]
        public void Metrics_TimeOfT_ReturnsOriginalValue()
        {
            // Обёртка замера не должна менять результат функции.
            int result = PerformanceMetrics.Time("Test.Metrics.ReturnValue", () => 7 * 6);
            Assert.Equal(42, result);
        }

        [Fact]
        public void Metrics_CountsErrorsButRethrowsException()
        {
            const string name = "Test.Metrics.Error";
            PerformanceMetrics.Reset();

            // Исключение должно пробрасываться наружу (поведение кода не меняется)...
            Assert.Throws<InvalidOperationException>(
                () => PerformanceMetrics.Time(name, () => throw new InvalidOperationException()));

            // ...но при этом засчитываться в статистику ошибок.
            var snapshot = PerformanceMetrics.Snapshot().Single(m => m.Name == name);
            Assert.Equal(1, snapshot.Errors);
        }

        [Fact]
        public void Metrics_RejectsEmptyName()
        {
            Assert.Throws<ArgumentException>(() => PerformanceMetrics.Time("", () => { }));
        }
    }
}
