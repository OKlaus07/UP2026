using ClubRegistration.Mvc.Models;
using ClubRegistration.Mvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ClubRegistration.Mvc.Controllers
{
    [Authorize]
    public class FamilyController : Controller
    {
        public IActionResult Index()
        {
            try { return View(new FamilyService().Load(UserId())); }
            catch (Exception) { TempData["Error"] = "Не удалось загрузить семейный раздел. Проверьте миграцию БД."; return View(new FamilyPageModel()); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Invite(ParentInviteModel model)
        {
            if (!ModelState.IsValid) TempData["Error"] = "Введите корректную почту зарегистрированного ученика.";
            else
            {
                try { var error = new FamilyService().InviteStudent(UserId(), model.StudentEmail); TempData[error == null ? "Message" : "Error"] = error ?? "Запрос отправлен. Ученик должен подтвердить связь из своего аккаунта."; }
                catch (Exception) { TempData["Error"] = "Не удалось отправить запрос родителя."; }
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Decide(long id, bool accept)
        {
            try { var error = new FamilyService().DecideInvite(UserId(), id, accept); TempData[error == null ? "Message" : "Error"] = error ?? (accept ? "Связь с родителем подтверждена." : "Запрос отклонён."); }
            catch (Exception) { TempData["Error"] = "Не удалось обработать запрос."; }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SaveStudent(FamilyStudentEditModel model)
        {
            if (!ModelState.IsValid) TempData["Error"] = "Проверьте имя, фамилию и телефон ученика.";
            else
            {
                try { var error = new FamilyService().UpdateStudent(UserId(), model); TempData[error == null ? "Message" : "Error"] = error ?? "Данные ученика обновлены."; }
                catch (Exception) { TempData["Error"] = "Не удалось обновить данные ученика."; }
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelEnrollment(int id)
        {
            try { var error = new EnrollmentService().CancelForParent(id, UserId()); TempData[error == null ? "Message" : "Error"] = error ?? "Заявка ученика отменена."; }
            catch (Exception) { TempData["Error"] = "Не удалось отменить заявку ученика."; }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoveLink(long id)
        {
            try { if (new FamilyService().RemoveLink(UserId(), id)) TempData["Message"] = "Связь с учеником удалена."; else TempData["Error"] = "Связь не найдена."; }
            catch (Exception) { TempData["Error"] = "Не удалось удалить семейную связь."; }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoveParent(long id)
        {
            try { if (new FamilyService().RemoveParent(UserId(), id)) TempData["Message"] = "Родительский доступ отозван."; else TempData["Error"] = "Подтверждённая связь не найдена."; }
            catch (Exception) { TempData["Error"] = "Не удалось отозвать родительский доступ."; }
            return RedirectToAction("Index");
        }

        private int UserId()
        {
            int id;
            if (!int.TryParse(User.Identity.Name, out id)) throw new InvalidOperationException("Не удалось определить пользователя.");
            return id;
        }
    }
}
