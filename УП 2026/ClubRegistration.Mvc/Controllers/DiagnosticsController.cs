// ============================================================
// DiagnosticsController.cs
// Контроллер диагностики для трёх индивидуальных заданий:
//
//  23.11.2026 — «Выбор средств разработки»: страница Index()
//      кратко фиксирует, какие средства выбраны для проекта
//      (C# / ASP.NET Core MVC / PostgreSQL / xUnit / Git).
//
//  24.11.2026 — «Модификация модулей и выявление ошибок»:
//      Benchmark() демонстрирует логирование и сравнение
//      «до/после» исправлений на алгоритмах сортировки.
//
//  25.11.2026 — «Быстродействие продукта»: Metrics() показывает
//      накопленные метрики PerformanceMetrics, Benchmark()
//      запускает прогон оптимизаций.
//
// Контроллер добавлен как отдельный файл и не меняет
// существующие маршруты, поэтому ничего не ломает.
// Доступ разрешён только администратору ([Authorize(Roles="admin")]),
// чтобы диагностика не была публичной страницей.
// ============================================================

using System;
using ClubRegistration.Mvc.Services.Optimization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ClubRegistration.Mvc.Controllers
{
    [Authorize(Roles = "admin")] // смотреть диагностику может только администратор сайта кружков
    public class DiagnosticsController : Controller
    {
        // Логгер внедряется через DI — это стандартный механизм
        // ASP.NET Core (задание 24.11: важность логирования).
        private readonly ILogger<DiagnosticsController> _logger;

        // Сервис бенчмарков тоже получаем из DI (зарегистрирован в Startup).
        private readonly PerformanceBenchmarks _benchmarks;

        public DiagnosticsController(
            ILogger<DiagnosticsController> logger,
            PerformanceBenchmarks benchmarks)
        {
            _logger = logger;
            _benchmarks = benchmarks;
        }

        // GET: /Diagnostics
        // Краткая сводка выбранных средств разработки (задание 23.11.2026).
        public IActionResult Index()
        {
            ViewBag.Tools = new[]
            {
                "Язык: C# 9 (.NET 5)",
                "Платформа: ASP.NET Core MVC",
                "СУБД: PostgreSQL + Npgsql",
                "IDE: Visual Studio 2019 / VS Code",
                "Тестирование: xUnit",
                "Контроль версий: Git"
            };
            return View();
        }

        // GET: /Diagnostics/Metrics
        // Таблица накопленных замеров производительности (задание 25.11.2026).
        public IActionResult Metrics()
        {
            var snapshot = PerformanceMetrics.Snapshot();
            _logger.LogInformation("Диагностика: просмотр метрик, записей={Count}", snapshot.Count);
            return View(snapshot);
        }

        // GET: /Diagnostics/Benchmark?items=50000
        // Запуск сравнения «наивный код vs оптимизированный».
        // Параметры защищены проверкой, чтобы пользователь не мог
        // задать отрицательное или слишком большое число (защита от DoS).
        [HttpGet]
        public IActionResult Benchmark(int items = 20000)
        {
            if (items < 1000) items = 1000;       // слишком мало — нерепрезентативно
            if (items > 200000) items = 200000;   // слишком много — риск зависания запроса

            try
            {
                string report = _benchmarks.RunFullComparison(items);
                ViewBag.Report = report;
                return View("Metrics", PerformanceMetrics.Snapshot());
            }
            catch (Exception ex)
            {
                // Исключение логируем, но пользователю показываем
                // понятное сообщение без деталей стека (безопасность).
                _logger.LogError(ex, "Ошибка при прогоне бенчмарка");
                TempData["Error"] = "Не удалось выполнить замер производительности.";
                return RedirectToAction(nameof(Metrics));
            }
        }
    }
}
