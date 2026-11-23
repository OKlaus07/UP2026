using Microsoft.AspNetCore.Mvc;

namespace ClubRegistration.Mvc.Controllers
{
    public class HomeController : Controller
    {
        // Понятная страница для непредвиденной ошибки в Production.
        [Route("Home/Error")]
        public IActionResult Error() { Response.StatusCode = 500; return View("~/Views/Shared/Error.cshtml"); }
    }
}
