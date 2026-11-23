
using System;
using System.IO;
using System.Threading.Tasks;

using ClubRegistration.Mvc.Services;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace ClubRegistration.Mvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Регистрируем MVC, cookie-вход и подключение к PostgreSQL из User Secrets.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            // Настройка хранения ключей Data Protection.
            string keyFolder = Path.Combine(
                AppContext.BaseDirectory,
                "App_Data",
                "DataProtectionKeys"
            );

            Directory.CreateDirectory(keyFolder);

            var dataProtection = services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyFolder));

            if (OperatingSystem.IsWindows())
            {
                // Ключи cookie шифруются средствами Windows и сохраняются между перезапусками.
                dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
            }

            // Настройка авторизации через Cookie.
            services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme
            )
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                // Деактивация аккаунта или смена роли действует и для уже открытых сессий.
                options.Events.OnValidatePrincipal = async context =>
                {
                    int userId;
                    string role = context.Principal == null ? null : context.Principal.FindFirst(ClaimTypes.Role)?.Value;
                    string idText = context.Principal == null ? null : context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    bool isAllowed = int.TryParse(idText, out userId);
                    if (isAllowed)
                    {
                        try { isAllowed = new AccountService().IsAccountAllowed(userId, role); }
                        catch { isAllowed = false; }
                    }
                    if (!isAllowed)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

            services.AddAuthorization();

            // Регистрация сервисов приложения.
            services.AddScoped<SiteSettingsService>();

            // Подключение PostgreSQL из User Secrets.
            Db.Configure(
                Configuration.GetConnectionString("DefaultConnection")
            );
        }

        // Настраиваем обработку ошибок, UTF-8 и маршруты сайта.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment environment
        )
        {
            if (string.Equals(
    environment.EnvironmentName,
    "Development",
    StringComparison.OrdinalIgnoreCase))
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Устанавливаем UTF-8 для текстовых ответов.
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    if (
                        context.Response.ContentType != null &&
                        context.Response.ContentType.StartsWith(
                            "text/",
                            StringComparison.OrdinalIgnoreCase
                        ) &&
                        !context.Response.ContentType.Contains(
                            "charset",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        context.Response.ContentType += "; charset=utf-8";
                    }

                    return Task.CompletedTask;
                });

                await next();
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Основной маршрут приложения.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Clubs}/{action=Index}/{id?}"
                );
            });
        }
    }
}
