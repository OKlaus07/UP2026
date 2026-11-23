
// ============================================================
// AccountController.cs
// Контроллер авторизации, регистрации и выхода пользователя
// ============================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

using ClubRegistration.Mvc.Models;
using ClubRegistration.Mvc.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubRegistration.Mvc.Controllers
{
    public class AccountController : Controller
    {
        // ----------------------------------------------------
        // GET: Account/Login
        // Отображение страницы входа
        // ----------------------------------------------------
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginModel());
        }

        // ----------------------------------------------------
        // POST: Account/Login
        // Проверка данных и создание авторизационной cookie
        // ----------------------------------------------------
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string role;
                string accountCategory;

                int? id = new AccountService().Login(
                    model.Email,
                    model.Password,
                    out role,
                    out accountCategory
                );

                if (!id.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        "Почта или пароль указаны неверно."
                    );

                    return View(model);
                }

                // Создаём claims на основании данных из БД.
                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        id.Value.ToString()
                    ),

                    new Claim(
                        ClaimTypes.Name,
                        id.Value.ToString()
                    ),

                    new Claim(
                        ClaimTypes.Role,
                        role
                    ),
                    new Claim("account_category", accountCategory ?? "student")
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var principal = new ClaimsPrincipal(identity);

                var properties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    properties
                );

                return RedirectToAction("Index", "Clubs");
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Не удалось выполнить вход. Проверьте подключение к базе данных."
                );

                return View(model);
            }
        }

        // ----------------------------------------------------
        // GET: Account/Register
        // Отображение страницы регистрации
        // ----------------------------------------------------
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewBag.AccountCategories = AccountCategories();
            return View(new RegisterModel());
        }

        // ----------------------------------------------------
        // POST: Account/Register
        // Создание нового пользователя
        // ----------------------------------------------------
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AccountCategories = AccountCategories();
                return View(model);
            }

            try
            {
                string error = new AccountService().Register(model);

                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    ViewBag.AccountCategories = AccountCategories();
                    return View(model);
                }

                TempData["Message"] = "Аккаунт создан. Теперь войдите.";

                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Не удалось сохранить регистрацию. Проверьте подключение к PostgreSQL."
                );

                ViewBag.AccountCategories = AccountCategories();
                return View(model);
            }
        }

        // Список типов аккаунта для регистрации. Права администратора выбираются отдельно.
        private static Microsoft.AspNetCore.Mvc.Rendering.SelectList AccountCategories()
        {
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new[]
            {
                new { Value = "parent", Text = "Родитель" },
                new { Value = "student", Text = "Ученик" },
                new { Value = "college_student", Text = "Студент" },
                new { Value = "teacher", Text = "Преподаватель" }
            }, "Value", "Text");
        }

        // ----------------------------------------------------
        // POST: Account/Logout
        // Завершение пользовательской сессии
        // ----------------------------------------------------
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Index", "Clubs");
        }
    }

    // ========================================================
    // Модель формы входа
    // ========================================================
    public class LoginModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}
