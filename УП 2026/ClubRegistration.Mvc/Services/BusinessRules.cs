using System;
namespace ClubRegistration.Mvc.Services
{
    // Простые правила приложения вынесены отдельно, чтобы их можно было тестировать без PostgreSQL.
    public static class BusinessRules
    {
        // Проверяет название до отправки команды изменения в БД.
        public static string ValidateCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Введите название категории.";
            if (name.Trim().Length > 100) return "Название категории не должно превышать 100 символов.";
            return null;
        }

        public static string ValidateClub(string name,int minAge,int maxAge,decimal price,int capacity)
        {
            if(string.IsNullOrWhiteSpace(name))return "Введите название кружка.";
            if(minAge<0||maxAge<minAge)return "Проверьте возрастные ограничения.";
            if(price<0)return "Стоимость не может быть отрицательной.";
            if(capacity<1)return "Вместимость должна быть положительной.";
            return null;
        }
        public static bool IsScheduleValid(int day,TimeSpan start,TimeSpan end){return day>=1&&day<=7&&start>=TimeSpan.Zero&&end<=TimeSpan.FromDays(1)&&start<end;}
        public static bool HasScheduleConflict(int day,TimeSpan start,TimeSpan end,int otherDay,TimeSpan otherStart,TimeSpan otherEnd){return day==otherDay&&start<otherEnd&&otherStart<end;}
        public static bool CanApprove(string status,int approved,int capacity){return status=="pending"&&capacity>approved;}
    }
}
