using System;
namespace ClubRegistration.Mvc.Models
{
    public class EnrollmentRow
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ClubName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}
