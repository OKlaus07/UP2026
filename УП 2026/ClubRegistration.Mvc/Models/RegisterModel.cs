using System.ComponentModel.DataAnnotations;
namespace ClubRegistration.Mvc.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage="Введите имя")][Display(Name="Имя")] public string FirstName { get; set; }
        [Required(ErrorMessage="Введите фамилию")][Display(Name="Фамилия")] public string LastName { get; set; }
        [Display(Name="Отчество")] public string MiddleName { get; set; }
        [Required, EmailAddress(ErrorMessage="Укажите корректную почту")][Display(Name="Электронная почта")] public string Email { get; set; }
        [Required(ErrorMessage="Введите телефон")][Display(Name="Телефон")] public string Phone { get; set; }
        [Required(ErrorMessage="Выберите категорию аккаунта")][RegularExpression("^(parent|student|college_student|teacher)$", ErrorMessage="Выберите категорию из списка")][Display(Name="Категория")] public string AccountCategory { get; set; }
        [Required, MinLength(8,ErrorMessage="Минимум 8 символов")][DataType(DataType.Password)][Display(Name="Пароль")] public string Password { get; set; }
        [Compare("Password",ErrorMessage="Пароли не совпадают")][DataType(DataType.Password)][Display(Name="Повторите пароль")] public string ConfirmPassword { get; set; }
    }
}
