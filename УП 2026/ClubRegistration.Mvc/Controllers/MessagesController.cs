using ClubRegistration.Mvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ClubRegistration.Mvc.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            try { return View(new ChatService().Load(UserId())); }
            catch (Exception) { TempData["Error"] = "Не удалось загрузить сообщения. Проверьте миграцию БД и связь преподавателя с аккаунтом."; return View(new ClubRegistration.Mvc.Models.ChatPageModel()); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Start(int studentId, int teacherId)
        {
            try { var id = new ChatService().Start(UserId(), studentId, teacherId); return RedirectToAction("Conversation", new { id }); }
            catch (Exception error) { TempData["Error"] = error is UnauthorizedAccessException || error is InvalidOperationException ? error.Message : "Не удалось открыть диалог."; return RedirectToAction("Index"); }
        }

        public IActionResult Conversation(long id)
        {
            try
            {
                var service = new ChatService();
                var model = service.Load(UserId());
                model.Messages = service.Read(UserId(), id, out var header);
                model.Current = header;
                return View(model);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception) { TempData["Error"] = "Не удалось открыть диалог."; return RedirectToAction("Index"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Send(long conversationId, string body)
        {
            try { new ChatService().Send(UserId(), conversationId, body); }
            catch (Exception error) { TempData["Error"] = error is ArgumentException || error is UnauthorizedAccessException ? error.Message : "Не удалось отправить сообщение."; }
            return RedirectToAction("Conversation", new { id = conversationId });
        }

        private int UserId()
        {
            int id;
            if (!int.TryParse(User.Identity.Name, out id)) throw new InvalidOperationException("Не удалось определить пользователя.");
            return id;
        }
    }
}
