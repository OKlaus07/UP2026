using System.Collections.Generic;
namespace ClubRegistration.Mvc.Models
{
    public class AdminDashboard
    {
        public List<AdminClubRow> Clubs {get;set;}
        public List<AdminTeacherRow> Teachers {get;set;}
        public List<AdminScheduleRow> Schedules {get;set;}
        public List<AdminUserRow> Users {get;set;}
        public List<string> Categories {get;set;}
        // Категории с идентификаторами нужны для их редактирования в панели.
        public List<AdminCategoryRow> CategoryRows {get;set;}
        public string Statistics {get;set;}
        public string Popular {get;set;}
        public AdminClubRow EditClub {get;set;}
        public AdminTeacherRow EditTeacher {get;set;}
        public AdminScheduleRow EditSchedule {get;set;}
    }
    public class AdminCategoryRow {public int Id{get;set;}public string Name{get;set;}public int ClubsCount{get;set;}}
    public class AdminClubRow {public int Id{get;set;}public string Name{get;set;}public string Category{get;set;}public int CategoryId{get;set;}public string Description{get;set;}public int MinAge{get;set;}public int MaxAge{get;set;}public decimal Price{get;set;}public int Capacity{get;set;}public int TeacherId{get;set;}public string Teacher{get;set;}public bool Active{get;set;}}
    public class AdminTeacherRow {public int Id{get;set;}public int UserId{get;set;}public string FirstName{get;set;}public string LastName{get;set;}public string MiddleName{get;set;}public string Specialty{get;set;}public string Phone{get;set;}public string Description{get;set;}public bool Active{get;set;}public string FullName{get{return FirstName+" "+(MiddleName??"")+" "+LastName;}}}
    public class AdminAccountChoice {public int Id{get;set;}public string Name{get;set;}}
    public class AdminScheduleRow {public int Id{get;set;}public int ClubId{get;set;}public int TeacherId{get;set;}public string Club{get;set;}public string Teacher{get;set;}public int Weekday{get;set;}public string Day{get;set;}public string Start{get;set;}public string End{get;set;}public string Room{get;set;}}
    public class AdminUserRow {public int Id{get;set;}public string FirstName{get;set;}public string LastName{get;set;}public string Name{get{return (FirstName+" "+LastName).Trim();}}public string Email{get;set;}public string Phone{get;set;}public string AccountCategory{get;set;}public string CategoryCode{get;set;}public string Role{get;set;}public bool Active{get;set;}}
    public class AdminEditUserModel
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.StringLength(80)] public string FirstName { get; set; }
        [System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.StringLength(80)] public string LastName { get; set; }
        [System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.EmailAddress][System.ComponentModel.DataAnnotations.StringLength(254)] public string Email { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(30)] public string Phone { get; set; }
        [System.ComponentModel.DataAnnotations.Required] public string AccountCategory { get; set; }
    }
    public class AdminCreateUserModel
    {
        [System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.StringLength(80)] public string FirstName { get; set; }
        [System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.StringLength(80)] public string LastName { get; set; }
        [System.ComponentModel.DataAnnotations.EmailAddress][System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.StringLength(254)] public string Email { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(30)] public string Phone { get; set; }
        [System.ComponentModel.DataAnnotations.Required][System.ComponentModel.DataAnnotations.MinLength(8)] public string Password { get; set; }
        [System.ComponentModel.DataAnnotations.Required] public string AccountCategory { get; set; }
        [System.ComponentModel.DataAnnotations.Required] public string Role { get; set; }
    }
}
