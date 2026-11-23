// ============================================================
// SortingAlgorithms.cs
// Практическая часть задания от 25.11.2026 (пункт 3.1 —
// алгоритмическая оптимизация).
//
// В приложении «Запись в кружки» есть «горячее» место —
// сортировка списка кружков по названию. Здесь показаны два
// варианта:
//   1) BubbleSort  — пузырьковая сортировка, O(n^2).
//      Оставлена как «до оптимизации», на её удобно ловить
//      типичные ошибки (см. задание от 24.11.2026).
//   2) QuickSort   — быстрая сортировка, O(n log n) в среднем.
//      Это вариант «после оптимизации».
//   3) SortedSetIndex — пример оптимизации структуры данных:
//      вместо повторной сортировки поддерживаем отсортированный
//      индекс и получаем выборку за O(log n).
//
// Модуль не зависит от БД и ASP.NET Core, его можно запускать
// из тестов и со страницы диагностики.
// ============================================================

using System;
using System.Collections.Generic;

namespace ClubRegistration.Mvc.Services.Optimization
{
    public static class SortingAlgorithms
    {
        // ------------------------------------------------------------
        // 1. Пузырьковая сортировка (ДО оптимизации), сложность O(n^2).
        //    Работает правильно, но медленно на больших списках.
        // ------------------------------------------------------------
        public static void BubbleSort(IList<string> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            int n = items.Count;
            for (int i = 0; i < n - 1; i++)
            {
                // Флаг swapped — маленькая оптимизация уже внутри
                // «плохого» алгоритма: если обменов не было, массив
                // уже отсортирован и дальше можно не идти.
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (string.CompareOrdinal(items[j], items[j + 1]) > 0)
                    {
                        string temp = items[j];
                        items[j] = items[j + 1];
                        items[j + 1] = temp;
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
        }

        // ------------------------------------------------------------
        // 2. Быстрая сортировка (ПОСЛЕ оптимизации), O(n log n) в среднем.
        //    Сортирует тот же список, что и BubbleSort, но значительно
        //    быстрее на больших объёмах данных.
        // ------------------------------------------------------------
        public static void QuickSort(IList<string> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            QuickSortRange(items, 0, items.Count - 1);
        }

        private static void QuickSortRange(IList<string> items, int low, int high)
        {
            while (low < high)
            {
                int pivotIndex = Partition(items, low, high);

                // Хвостовая рекурсия заменена циклом (tail-call
                // optimization): это уменьшает глубину стека вызовов
                // и защищает от StackOverflow на больших данных.
                if (pivotIndex - low < high - pivotIndex)
                {
                    QuickSortRange(items, low, pivotIndex - 1);
                    low = pivotIndex + 1;
                }
                else
                {
                    QuickSortRange(items, pivotIndex + 1, high);
                    high = pivotIndex - 1;
                }
            }
        }

        // Разбиение по схеме Хоара с выбором медианы из трёх.
        // Медиана защищает алгоритм от деградации до O(n^2) на
        // почти отсортированных данных (частый случай — список
        // кружков уже примерно отсортирован по алфавиту).
        private static int Partition(IList<string> items, int low, int high)
        {
            int mid = low + (high - low) / 2;
            if (string.CompareOrdinal(items[mid], items[low]) < 0) Swap(items, low, mid);
            if (string.CompareOrdinal(items[high], items[low]) < 0) Swap(items, low, high);
            if (string.CompareOrdinal(items[mid], items[high]) < 0) Swap(items, mid, high);
            // После трёх обменов медиана стоит в позиции mid — она и есть опора.
            Swap(items, mid, high);

            string pivot = items[high];
            int i = low;      // идёт слева направо
            int j = high - 1; // идёт справа налево
            while (true)
            {
                // Строгое «меньше» гарантирует завершение цикла
                // даже при одинаковых элементах.
                while (i <= j && string.CompareOrdinal(items[i], pivot) < 0) i++;
                while (j >= i && string.CompareOrdinal(items[j], pivot) > 0) j--;
                if (i >= j) break;
                Swap(items, i, j);
                i++;
                j--;
            }
            Swap(items, i, high); // ставим опору на своё место
            return i;
        }

        private static void Swap(IList<string> items, int a, int b)
        {
            string temp = items[a];
            items[a] = items[b];
            items[b] = temp;
        }

        // ------------------------------------------------------------
        // 3. Оптимизация структуры данных: отсортированный индекс.
        //
        // Идея: каталог кружков запрашивают часто, а меняют редко.
        // Вместо того чтобы каждый раз заново сортировать список
        // (O(n log n)), один раз кладём названия в HashSet для
        // быстрой проверки «есть ли такой кружок» (O(1)) и держим
        // отсортированный массив (получение — O(1)).
        //
        // Класс демонстрационный: показывает, что выбор структуры
        // данных часто даёт больший выигрыш, чем микрооптимизации.
        // ------------------------------------------------------------
        public sealed class SortedClubIndex
        {
            // HashSet — быстрый поиск наличия названия.
            private readonly HashSet<string> _names =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Отсортированный снимок для вывода на страницу.
            private string[] _sortedCache = Array.Empty<string>();

            // Флаг «кэш протух»: пересобираем массив только после
            // изменений — ленивая (отложенная) оптимизация.
            private bool _cacheIsDirty = true;

            /// <summary>Добавляет название кружка в индекс.</summary>
            public void Add(string clubName)
            {
                if (string.IsNullOrWhiteSpace(clubName))
                    throw new ArgumentException("Название кружка не может быть пустым.", nameof(clubName));

                // Если название уже есть, ничего не делаем —
                // дубликаты в индексе не нужны.
                if (_names.Add(clubName.Trim()))
                    _cacheIsDirty = true;
            }

            /// <summary>Есть ли кружок с таким названием (без учёта регистра).</summary>
            public bool Contains(string clubName)
            {
                return clubName != null && _names.Contains(clubName.Trim());
            }

            /// <summary>Количество уникальных кружков в индексе.</summary>
            public int Count { get { return _names.Count; } }

            /// <summary>
            /// Возвращает отсортированный список названий.
            /// Повторные вызовы без изменений — O(1), потому что
            /// используется готовый кэш.
            /// </summary>
            public IReadOnlyList<string> GetSorted()
            {
                if (_cacheIsDirty)
                {
                    var list = new List<string>(_names);
                    QuickSort(list);                 // переиспользуем свой быстрый алгоритм
                    _sortedCache = list.ToArray();
                    _cacheIsDirty = false;
                }
                return _sortedCache;
            }
        }
    }
}
