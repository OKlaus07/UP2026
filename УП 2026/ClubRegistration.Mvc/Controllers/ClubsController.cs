using ClubRegistration.Mvc.Services;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClubRegistration.Mvc.Controllers
{
    public class ClubsController : Controller
    {
        public IActionResult Index(string search,string category)
        {
            try{var service=new ClubService();ViewBag.Categories=service.Categories();ViewBag.Search=search;ViewBag.Category=category;return View(service.Search(search,category));}
            catch(Exception){TempData["Error"]="Не удалось загрузить каталог. Проверьте соединение с базой.";return View(new ClubServiceFallback().Empty());}
        }
        public IActionResult Schedule(){try{return View(new ScheduleService().All());}catch(Exception){TempData["Error"]="Не удалось загрузить расписание.";return View(new System.Collections.Generic.List<ClubRegistration.Mvc.Models.ScheduleRow>());}}
        [Authorize,HttpPost,ValidateAntiForgeryToken] public IActionResult Apply(int id)
        {
            try{var message=new ClubService().Apply(UserId(),id);TempData[message==null?"Message":"Error"]=message??"Заявка отправлена администратору.";}
            catch(Exception){TempData["Error"]="Не удалось отправить заявку.";}
            return RedirectToAction("Index");
        }
        [Authorize] public IActionResult Mine(){try{return View(new EnrollmentService().Mine(UserId()));}catch(Exception){TempData["Error"]="Не удалось загрузить заявки.";return View(new EnrollmentServiceFallback().Empty());}}
        [Authorize,HttpPost,ValidateAntiForgeryToken] public IActionResult Cancel(int id)
        {
            try{var error=new EnrollmentService().Cancel(id,UserId());TempData[error==null?"Message":"Error"]=error??"Заявка отменена.";}catch(Exception){TempData["Error"]="Не удалось отменить заявку.";}
            return RedirectToAction("Mine");
        }
        private int UserId(){int id;if(!int.TryParse(User.Identity.Name,out id))throw new InvalidOperationException("Не удалось определить пользователя.");return id;}
    }
}
