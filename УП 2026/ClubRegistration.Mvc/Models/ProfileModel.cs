using System.ComponentModel.DataAnnotations;
namespace ClubRegistration.Mvc.Models
{
    public class ProfileModel
    {
        [Required,Display(Name="Имя")] public string FirstName{get;set;}
        [Required,Display(Name="Фамилия")] public string LastName{get;set;}
        [Display(Name="Отчество")] public string MiddleName{get;set;}
        [Required,Phone,Display(Name="Телефон")] public string Phone{get;set;}
        [Display(Name="Электронная почта")] public string Email{get;set;}
    }
    public class PasswordChangeModel
    {
        [Required,DataType(DataType.Password),Display(Name="Старый пароль")] public string OldPassword{get;set;}
        [Required,MinLength(8),DataType(DataType.Password),Display(Name="Новый пароль")] public string NewPassword{get;set;}
        [Compare("NewPassword"),DataType(DataType.Password),Display(Name="Повторите новый пароль")] public string ConfirmPassword{get;set;}
    }
}
