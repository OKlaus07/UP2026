using ClubRegistration.Mvc.Services;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClubRegistration.Mvc.Controllers
{
    public class ThemeController : Controller
    {
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public IActionResult Toggle(string returnUrl)
        {
            int? userId = null;
            if (User.Identity.IsAuthenticated && int.TryParse(User.Identity.Name, out int parsedId)) userId = parsedId;
            var settings = new SiteSettingsService();
            string current = settings.GetUserTheme(userId, Request.Cookies["site-theme"]);
            string selected = current == "dark" ? "light" : "dark";

            Response.Cookies.Append("site-theme", selected, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

            if (userId.HasValue)
            {
                try { settings.SaveUserTheme(userId.Value, selected); }
                catch { TempData["Error"] = "Тема переключена в браузере, но не записалась в профиль. Примените SQL-скрипт обновления БД."; }
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Clubs");
        }
    }
}
