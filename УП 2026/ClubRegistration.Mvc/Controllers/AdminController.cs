using ClubRegistration.Mvc.Services;
using ClubRegistration.Mvc.Models;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace ClubRegistration.Mvc.Controllers
{
    [Authorize(Roles="admin")]
    public class AdminController : Controller
    {
        public IActionResult Manage(int? club,int? teacher,int? schedule)
        {
            try{var service=new AdminService();var model=service.Load(club,teacher,schedule);ViewBag.Categories=new SelectList(service.Categories(),"Key","Value",model.EditClub==null?0:model.EditClub.CategoryId);ViewBag.Teachers=new SelectList(model.Teachers.Where(x=>x.Active),"Id","FullName",model.EditClub==null?0:model.EditClub.TeacherId);ViewBag.Clubs=new SelectList(model.Clubs.Where(x=>x.Active),"Id","Name",model.EditSchedule==null?0:model.EditSchedule.ClubId);ViewBag.ScheduleTeachers=new SelectList(model.Teachers.Where(x=>x.Active),"Id","FullName",model.EditSchedule==null?0:model.EditSchedule.TeacherId);ViewBag.TeacherAccounts=new SelectList(service.TeacherAccounts(),"Id","Name",model.EditTeacher==null?0:model.EditTeacher.UserId);return View(model);}
            catch(Exception){TempData["Error"]="Не удалось загрузить справочники.";var emptyTeachers=new System.Collections.Generic.List<ClubRegistration.Mvc.Models.AdminTeacherRow>();var emptyClubs=new System.Collections.Generic.List<ClubRegistration.Mvc.Models.AdminClubRow>();ViewBag.Categories=new SelectList(new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int,string>>(),"Key","Value");ViewBag.Teachers=new SelectList(emptyTeachers,"Id","FullName");ViewBag.ScheduleTeachers=new SelectList(emptyTeachers,"Id","FullName");ViewBag.TeacherAccounts=new SelectList(new System.Collections.Generic.List<AdminAccountChoice>(),"Id","Name");ViewBag.Clubs=new SelectList(emptyClubs,"Id","Name");return View(new AdminDashboard{Clubs=emptyClubs,Teachers=emptyTeachers,Schedules=new System.Collections.Generic.List<AdminScheduleRow>(),Users=new System.Collections.Generic.List<AdminUserRow>(),Categories=new System.Collections.Generic.List<string>(),CategoryRows=new System.Collections.Generic.List<AdminCategoryRow>()});}
        }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SaveCategory(int id,string name){if(!ModelState.IsValid){TempData["Error"]="Проверьте идентификатор категории.";return RedirectToAction("Manage");}try{new AdminService().SaveCategory(id,name);TempData["Message"]=id==0?"Категория добавлена.":"Название категории изменено.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteCategory(int id){try{if(new AdminService().DeleteCategory(id))TempData["Message"]="Категория удалена.";else TempData["Error"]="Нельзя удалить категорию, пока к ней привязаны кружки.";}catch(Exception){TempData["Error"]="Не удалось удалить категорию.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SaveClub(ClubRegistration.Mvc.Models.AdminClubRow item){if(!ModelState.IsValid){TempData["Error"]="Проверьте числовые поля кружка.";return RedirectToAction("Manage");}try{new AdminService().SaveClub(item);TempData["Message"]="Кружок сохранён.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeactivateClub(int id){try{if(new AdminService().DeactivateClub(id))TempData["Message"]="Кружок деактивирован.";else TempData["Error"]="Кружок не найден.";}catch(Exception){TempData["Error"]="Не удалось деактивировать кружок.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult ActivateClub(int id){try{if(new AdminService().ActivateClub(id))TempData["Message"]="Кружок снова доступен в каталоге.";else TempData["Error"]="Кружок не найден.";}catch(Exception){TempData["Error"]="Не удалось включить кружок.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteClub(int id){try{if(!new AdminService().DeleteClub(id))TempData["Error"]="Нельзя удалить кружок, используемый в заявках.";else TempData["Message"]="Кружок удалён.";}catch(Exception){TempData["Error"]="Не удалось удалить кружок.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SaveTeacher(ClubRegistration.Mvc.Models.AdminTeacherRow item){if(!ModelState.IsValid){TempData["Error"]="Проверьте введённые данные преподавателя.";return RedirectToAction("Manage");}try{new AdminService().SaveTeacher(item);TempData["Message"]="Преподаватель сохранён.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeactivateTeacher(int id){try{if(new AdminService().DeactivateTeacher(id))TempData["Message"]="Преподаватель деактивирован.";else TempData["Error"]="Преподаватель не найден.";}catch(Exception){TempData["Error"]="Не удалось изменить преподавателя.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult ActivateTeacher(int id){try{if(new AdminService().ActivateTeacher(id))TempData["Message"]="Преподаватель снова активен.";else TempData["Error"]="Преподаватель не найден.";}catch(Exception){TempData["Error"]="Не удалось включить преподавателя.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SaveSchedule(ClubRegistration.Mvc.Models.AdminScheduleRow item){if(!ModelState.IsValid){TempData["Error"]="Проверьте числовые поля расписания.";return RedirectToAction("Manage");}try{new AdminService().SaveSchedule(item);TempData["Message"]="Расписание сохранено.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteSchedule(int id){try{if(new AdminService().DeleteSchedule(id))TempData["Message"]="Занятие удалено из расписания.";else TempData["Error"]="Занятие не найдено.";}catch(Exception){TempData["Error"]="Не удалось удалить занятие.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeactivateUser(int id){try{if(new AdminService().DeactivateUser(id))TempData["Message"]="Учётная запись деактивирована.";else TempData["Error"]="Пользователь не найден или это администратор.";}catch(Exception){TempData["Error"]="Не удалось деактивировать запись.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult ActivateUser(int id){try{if(new AdminService().ActivateUser(id))TempData["Message"]="Учётная запись снова активна.";else TempData["Error"]="Пользователь не найден или это администратор.";}catch(Exception){TempData["Error"]="Не удалось включить учётную запись.";}return RedirectToAction("Manage");}
        [HttpPost,ValidateAntiForgeryToken] public IActionResult CreateUser(AdminCreateUserModel item)
        {
            if(!ModelState.IsValid){TempData["Error"]="Заполните все поля и укажите пароль не короче 8 символов.";return RedirectToAction("Manage");}
            try{new AdminService().CreateUser(item);TempData["Message"]="Учётная запись создана.";}catch(ArgumentException error){TempData["Error"]=error.Message;}catch(Exception){TempData["Error"]="Не удалось создать учётную запись. Проверьте подключение и миграцию базы данных.";}
            return RedirectToAction("Manage");
        }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SaveUser(AdminEditUserModel item)
        {
            if(!ModelState.IsValid){TempData["Error"]="Проверьте имя, почту, телефон и категорию.";return RedirectToAction("Manage");}
            try{new AdminService().UpdateUser(item);TempData["Message"]="Данные аккаунта сохранены.";}catch(ArgumentException error){TempData["Error"]=error.Message;}catch(Exception){TempData["Error"]="Не удалось сохранить аккаунт.";}
            return RedirectToAction("Manage");
        }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SetUserRole(int id,string role)
        {
            if(id==UserId()){TempData["Error"]="Нельзя менять собственную роль во время текущего сеанса.";return RedirectToAction("Manage");}
            try{if(new AdminService().SetUserRole(id,role))TempData["Message"]="Роль пользователя изменена.";else TempData["Error"]="Не удалось изменить роль: проверьте пользователя и убедитесь, что остаётся активный администратор.";}catch(Exception){TempData["Error"]="Не удалось изменить роль.";}
            return RedirectToAction("Manage");
        }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteUser(int id)
        {
            if(id==UserId()){TempData["Error"]="Нельзя удалить собственную учётную запись администратора.";return RedirectToAction("Manage");}
            try{if(new AdminService().DeleteUser(id))TempData["Message"]="Учётная запись удалена.";else TempData["Error"]="Аккаунт не найден, имеет роль администратора или связан с заявками. Для сохранения истории заявок такую запись можно деактивировать.";}catch(Exception){TempData["Error"]="Не удалось удалить учётную запись.";}
            return RedirectToAction("Manage");
        }
        public IActionResult Requests(){try{return View(new EnrollmentService().All());}catch(Exception){TempData["Error"]="Не удалось загрузить заявки.";return View(new EnrollmentServiceFallback().Empty());}}
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleChristmasTheme(bool enabled)
        {
            try { new SiteSettingsService().SetChristmasTheme(enabled, UserId()); TempData["Message"] = enabled ? "Новогодняя тема включена." : "Новогодняя тема выключена."; }
            catch (Exception) { TempData["Error"] = "Не удалось сохранить настройку темы. Проверьте, что применён SQL-скрипт 04_safe_upgrade.sql."; }
            return RedirectToAction("Manage");
        }
        private int UserId() { return int.Parse(User.Identity.Name); }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult SetStatus(int id,string status)
        {
            try{var error=new EnrollmentService().SetStatus(id,status,UserId());TempData[error==null?"Message":"Error"]=error??"Статус обновлён.";}catch(Exception){TempData["Error"]="Не удалось изменить статус.";}
            return RedirectToAction("Requests");
        }
    }
}
