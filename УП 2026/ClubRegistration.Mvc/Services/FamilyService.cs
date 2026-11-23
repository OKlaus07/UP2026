using ClubRegistration.Mvc.Models;
using Npgsql;
using System;

namespace ClubRegistration.Mvc.Services
{
    // Связь родителя и ученика подтверждает именно ученик из своего аккаунта.
    public class FamilyService
    {
        public FamilyPageModel Load(int userId)
        {
            var model = new FamilyPageModel();
            using (var connection = Db.Open())
            {
                using (var command = new NpgsqlCommand("SELECT account_category FROM users WHERE id=@id AND is_active=true", connection))
                {
                    command.Parameters.AddWithValue("id", userId);
                    model.AccountCategory = Convert.ToString(command.ExecuteScalar());
                }

                if (model.AccountCategory == "parent")
                {
                    model.IsParent = true;
                    using (var command = new NpgsqlCommand(@"SELECT l.id,u.id,u.first_name||' '||u.last_name,u.email,l.status,u.first_name,u.last_name,COALESCE(u.phone,'')
                        FROM parent_student_links l JOIN users u ON u.id=l.student_user_id
                        WHERE l.parent_user_id=@id ORDER BY l.created_at DESC", connection))
                    {
                        command.Parameters.AddWithValue("id", userId);
                        using (var reader = command.ExecuteReader())
                            while (reader.Read()) model.Links.Add(new FamilyLinkRow { Id=reader.GetInt64(0),OtherUserId=reader.GetInt32(1),Name=reader.GetString(2),Email=reader.GetString(3),Status=reader.GetString(4),FirstName=reader.GetString(5),LastName=reader.GetString(6),Phone=reader.GetString(7) });
                    }
                    using (var command = new NpgsqlCommand(@"SELECT e.id,u.id,u.first_name||' '||u.last_name,c.name,e.status
                        FROM parent_student_links l JOIN enrollments e ON e.user_id=l.student_user_id
                        JOIN users u ON u.id=e.user_id JOIN clubs c ON c.id=e.club_id
                        WHERE l.parent_user_id=@id AND l.status='active'
                        ORDER BY u.last_name,c.name,e.created_at DESC", connection))
                    {
                        command.Parameters.AddWithValue("id", userId);
                        using (var reader = command.ExecuteReader())
                            while (reader.Read()) model.Enrollments.Add(new FamilyEnrollmentRow { Id=reader.GetInt32(0),StudentId=reader.GetInt32(1),StudentName=reader.GetString(2),ClubName=reader.GetString(3),Status=RussianStatus(reader.GetString(4)) });
                    }
                }
                else if (model.AccountCategory == "student")
                {
                    using (var command = new NpgsqlCommand(@"SELECT l.id,u.id,u.first_name||' '||u.last_name,u.email,l.status,u.first_name,u.last_name,COALESCE(u.phone,'')
                        FROM parent_student_links l JOIN users u ON u.id=l.parent_user_id
                        WHERE l.student_user_id=@id ORDER BY l.created_at DESC", connection))
                    {
                        command.Parameters.AddWithValue("id", userId);
                        using (var reader = command.ExecuteReader())
                            while (reader.Read()) model.Links.Add(new FamilyLinkRow { Id=reader.GetInt64(0),OtherUserId=reader.GetInt32(1),Name=reader.GetString(2),Email=reader.GetString(3),Status=reader.GetString(4),FirstName=reader.GetString(5),LastName=reader.GetString(6),Phone=reader.GetString(7) });
                    }
                }
            }
            return model;
        }

        public string InviteStudent(int parentId, string email)
        {
            using (var connection = Db.Open())
            using (var transaction = connection.BeginTransaction())
            {
                int studentId;
                using (var find = new NpgsqlCommand("SELECT id FROM users WHERE lower(email)=lower(@email) AND account_category='student' AND is_active=true", connection, transaction))
                {
                    find.Parameters.AddWithValue("email", (email ?? "").Trim());
                    var result = find.ExecuteScalar();
                    if (result == null) return "Активный аккаунт ученика с таким адресом не найден.";
                    studentId = Convert.ToInt32(result);
                }
                using (var check = new NpgsqlCommand("SELECT count(*) FROM users WHERE id=@id AND account_category='parent' AND is_active=true", connection, transaction))
                {
                    check.Parameters.AddWithValue("id", parentId);
                    if (Convert.ToInt32(check.ExecuteScalar()) != 1) return "Функция доступна только активному аккаунту родителя.";
                }
                using (var add = new NpgsqlCommand(@"INSERT INTO parent_student_links(parent_user_id,student_user_id)
                    VALUES(@parent,@student) ON CONFLICT(parent_user_id,student_user_id) DO NOTHING", connection, transaction))
                {
                    add.Parameters.AddWithValue("parent", parentId);
                    add.Parameters.AddWithValue("student", studentId);
                    if (add.ExecuteNonQuery() == 0) return "Приглашение или связь с этим учеником уже существует.";
                }
                using (var notice = new NpgsqlCommand("INSERT INTO notifications(user_id,message) VALUES(@student,@message)", connection, transaction))
                {
                    notice.Parameters.AddWithValue("student", studentId);
                    notice.Parameters.AddWithValue("message", "Родитель просит связать аккаунты. Проверьте запрос в разделе «Семья».");
                    notice.ExecuteNonQuery();
                }
                transaction.Commit();
                return null;
            }
        }

        public string DecideInvite(int studentId, long linkId, bool accept)
        {
            using (var connection = Db.Open())
            using (var transaction = connection.BeginTransaction())
            {
                using (var update = new NpgsqlCommand(@"UPDATE parent_student_links SET status=@status,decided_at=now()
                    WHERE id=@link AND student_user_id=@student AND status='pending'", connection, transaction))
                {
                    update.Parameters.AddWithValue("status", accept ? "active" : "rejected");
                    update.Parameters.AddWithValue("link", linkId);
                    update.Parameters.AddWithValue("student", studentId);
                    if (update.ExecuteNonQuery() != 1) return "Запрос не найден или уже обработан.";
                }
                if (accept)
                {
                    using (var notice = new NpgsqlCommand(@"INSERT INTO notifications(user_id,message)
                        SELECT parent_user_id,'Ученик подтвердил связь аккаунтов.' FROM parent_student_links WHERE id=@id", connection, transaction))
                    { notice.Parameters.AddWithValue("id", linkId); notice.ExecuteNonQuery(); }
                }
                transaction.Commit();
                return null;
            }
        }

        public bool RemoveLink(int parentId, long linkId)
        {
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand("DELETE FROM parent_student_links WHERE id=@link AND parent_user_id=@parent", connection))
            {
                command.Parameters.AddWithValue("link", linkId);
                command.Parameters.AddWithValue("parent", parentId);
                return command.ExecuteNonQuery() == 1;
            }
        }

        // Ученик может в любой момент отозвать ранее выданный родительский доступ.
        public bool RemoveParent(int studentId, long linkId)
        {
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand("DELETE FROM parent_student_links WHERE id=@link AND student_user_id=@student AND status='active'", connection))
            {
                command.Parameters.AddWithValue("link", linkId);
                command.Parameters.AddWithValue("student", studentId);
                return command.ExecuteNonQuery() == 1;
            }
        }

        public string UpdateStudent(int parentId, FamilyStudentEditModel model)
        {
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand(@"UPDATE users SET first_name=@first,last_name=@last,phone=@phone
                WHERE id=@student AND account_category='student' AND is_active=true AND EXISTS(
                    SELECT 1 FROM parent_student_links WHERE parent_user_id=@parent AND student_user_id=@student AND status='active')", connection))
            {
                command.Parameters.AddWithValue("first", model.FirstName.Trim());
                command.Parameters.AddWithValue("last", model.LastName.Trim());
                command.Parameters.AddWithValue("phone", string.IsNullOrWhiteSpace(model.Phone) ? (object)DBNull.Value : model.Phone.Trim());
                command.Parameters.AddWithValue("student", model.StudentId);
                command.Parameters.AddWithValue("parent", parentId);
                return command.ExecuteNonQuery() == 1 ? null : "Нет подтверждённого родительского доступа к этому ученику.";
            }
        }

        private string RussianStatus(string value)
        {
            switch (value)
            {
                case "pending": return "Ожидает рассмотрения";
                case "approved": return "Одобрена";
                case "rejected": return "Отклонена";
                default: return "Отменена";
            }
        }
    }
}
