using ClubRegistration.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// В точке входа запускается ASP.NET Core и настройки из Startup.
Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => { logging.ClearProviders(); logging.AddConsole(); })
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
    .Build()
    .Run();
