using ClubRegistration.Mvc.Models;
using ClubRegistration.Mvc.Services;
using Npgsql;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClubRegistration.Mvc.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            try { using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT first_name,last_name,middle_name,phone,email FROM users WHERE id=@id",connection)){command.Parameters.AddWithValue("id",UserId());using(var reader=command.ExecuteReader()){if(!reader.Read())return RedirectToAction("Login","Account");return View(new ProfileModel{FirstName=reader.GetString(0),LastName=reader.GetString(1),MiddleName=reader.IsDBNull(2)?"":reader.GetString(2),Phone=reader.IsDBNull(3)?"":reader.GetString(3),Email=reader.GetString(4)});}} }
            catch(Exception){TempData["Error"]="Не удалось загрузить профиль.";return View(new ProfileModel());}
        }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult Save(ProfileModel model)
        {
            if(!ModelState.IsValid)return View("Index",model);
            try{using(var connection=Db.Open())using(var command=new NpgsqlCommand("UPDATE users SET first_name=@first,last_name=@last,middle_name=@middle,phone=@phone WHERE id=@id",connection)){command.Parameters.AddWithValue("first",model.FirstName.Trim());command.Parameters.AddWithValue("last",model.LastName.Trim());command.Parameters.AddWithValue("middle",string.IsNullOrWhiteSpace(model.MiddleName)?(object)DBNull.Value:model.MiddleName.Trim());command.Parameters.AddWithValue("phone",model.Phone.Trim());command.Parameters.AddWithValue("id",UserId());command.ExecuteNonQuery();}TempData["Message"]="Личные данные сохранены.";return RedirectToAction("Index");}
            catch(Exception){ModelState.AddModelError("","Не удалось сохранить данные.");return View("Index",model);}
        }
        [HttpPost,ValidateAntiForgeryToken] public IActionResult ChangePassword(PasswordChangeModel model)
        {
            if(!ModelState.IsValid){TempData["Error"]="Заполните все поля смены пароля корректно.";return RedirectToAction("Index");}
            try{string oldHash;byte[] oldSalt;using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT password_hash,password_salt FROM users WHERE id=@id",connection)){command.Parameters.AddWithValue("id",UserId());using(var reader=command.ExecuteReader()){if(!reader.Read()){TempData["Error"]="Аккаунт не найден.";return RedirectToAction("Index");}oldHash=reader.GetString(0);oldSalt=(byte[])reader[1];}}if(!PasswordService.Verify(model.OldPassword,oldSalt,oldHash)){TempData["Error"]="Старый пароль указан неверно.";return RedirectToAction("Index");}var newSalt=PasswordService.NewSalt();var newHash=PasswordService.Hash(model.NewPassword,newSalt);using(var connection=Db.Open())using(var command=new NpgsqlCommand("UPDATE users SET password_hash=@hash,password_salt=@salt WHERE id=@id",connection)){command.Parameters.AddWithValue("hash",newHash);command.Parameters.AddWithValue("salt",newSalt);command.Parameters.AddWithValue("id",UserId());command.ExecuteNonQuery();}TempData["Message"]="Пароль изменён.";return RedirectToAction("Index");}
            catch(Exception){TempData["Error"]="Не удалось изменить пароль.";return RedirectToAction("Index");}
        }
        private int UserId(){int id;if(!int.TryParse(User.Identity.Name,out id))throw new InvalidOperationException();return id;}
    }
}
