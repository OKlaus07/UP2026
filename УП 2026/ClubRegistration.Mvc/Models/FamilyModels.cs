using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClubRegistration.Mvc.Models
{
    // Данные семейного кабинета: приглашения, привязанные дети и заявки.
    public class FamilyPageModel
    {
        public bool IsParent { get; set; }
        public string AccountCategory { get; set; }
        public List<FamilyLinkRow> Links { get; set; } = new List<FamilyLinkRow>();
        public List<FamilyEnrollmentRow> Enrollments { get; set; } = new List<FamilyEnrollmentRow>();
    }

    public class FamilyLinkRow
    {
        public long Id { get; set; }
        public int OtherUserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
    }

    public class FamilyEnrollmentRow
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClubName { get; set; }
        public string Status { get; set; }
    }

    public class FamilyStudentEditModel
    {
        [Required] public int StudentId { get; set; }
        [Required, StringLength(80)] public string FirstName { get; set; }
        [Required, StringLength(80)] public string LastName { get; set; }
        [Phone, StringLength(30)] public string Phone { get; set; }
    }

    public class ParentInviteModel
    {
        [Required, EmailAddress, StringLength(254)]
        [Display(Name = "Электронная почта ученика")]
        public string StudentEmail { get; set; }
    }

    public class ChatPageModel
    {
        public List<ChatConversationRow> Conversations { get; set; } = new List<ChatConversationRow>();
        public ChatConversationRow Current { get; set; }
        public List<ChatMessageRow> Messages { get; set; } = new List<ChatMessageRow>();
        public bool CanStart { get; set; }
        public List<ChatTeacherChoice> Teachers { get; set; } = new List<ChatTeacherChoice>();
    }

    public class ChatConversationRow
    {
        public long Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string TeacherName { get; set; }
        public string LastMessage { get; set; }
    }

    public class ChatMessageRow
    {
        public string SenderName { get; set; }
        public bool IsMine { get; set; }
        public string Body { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }

    public class ChatTeacherChoice
    {
        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public string Name { get; set; }
    }
}
