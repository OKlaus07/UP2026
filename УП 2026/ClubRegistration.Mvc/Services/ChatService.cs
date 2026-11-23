using ClubRegistration.Mvc.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace ClubRegistration.Mvc.Services
{
    // Чат доступен только родителю подтверждённого ученика и связанному преподавателю.
    public class ChatService
    {
        public ChatPageModel Load(int userId)
        {
            var model = new ChatPageModel();
            using (var connection = Db.Open())
            {
                string category;
                using (var account = new NpgsqlCommand("SELECT account_category FROM users WHERE id=@id AND is_active=true", connection))
                {
                    account.Parameters.AddWithValue("id", userId);
                    category = Convert.ToString(account.ExecuteScalar());
                }
                model.CanStart = category == "parent";
                string predicate = category == "parent"
                    ? "EXISTS(SELECT 1 FROM parent_student_links l WHERE l.parent_user_id=@user AND l.student_user_id=c.student_user_id AND l.status='active')"
                    : "t.user_id=@user";
                using (var command = new NpgsqlCommand(@"SELECT c.id,c.student_user_id,s.first_name||' '||s.last_name,
                    t.first_name||' '||t.last_name,COALESCE((SELECT m.body FROM chat_messages m WHERE m.conversation_id=c.id ORDER BY m.created_at DESC,m.id DESC LIMIT 1),'Диалог создан')
                    FROM chat_conversations c JOIN users s ON s.id=c.student_user_id
                    JOIN teachers t ON t.id=c.teacher_id WHERE " + predicate + " ORDER BY c.created_at DESC", connection))
                {
                    command.Parameters.AddWithValue("user", userId);
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) model.Conversations.Add(new ChatConversationRow { Id=reader.GetInt64(0),StudentId=reader.GetInt32(1),StudentName=reader.GetString(2),TeacherName=reader.GetString(3),LastMessage=reader.GetString(4) });
                }
                if (model.CanStart)
                {
                    using (var command = new NpgsqlCommand(@"SELECT DISTINCT l.student_user_id,t.id,s.first_name||' '||s.last_name||' — '||t.first_name||' '||t.last_name
                        FROM parent_student_links l JOIN users s ON s.id=l.student_user_id
                        JOIN enrollments e ON e.user_id=s.id AND e.status IN ('pending','approved')
                        JOIN clubs c ON c.id=e.club_id JOIN teachers t ON t.id=c.teacher_id
                        JOIN users tu ON tu.id=t.user_id AND tu.account_category='teacher' AND tu.is_active=true
                        WHERE l.parent_user_id=@user AND l.status='active' AND t.is_active=true
                        ORDER BY l.student_user_id,t.id", connection))
                    {
                        command.Parameters.AddWithValue("user", userId);
                        using (var reader = command.ExecuteReader())
                            while (reader.Read()) model.Teachers.Add(new ChatTeacherChoice { StudentId=reader.GetInt32(0),TeacherId=reader.GetInt32(1),Name=reader.GetString(2) });
                    }
                }
            }
            return model;
        }

        public long Start(int parentId, int studentId, int teacherId)
        {
            using (var connection = Db.Open())
            using (var transaction = connection.BeginTransaction())
            {
                int teacherUserId = GetTeacherUserId(connection, transaction, teacherId);
                using (var check = new NpgsqlCommand(@"SELECT count(*) FROM users p
                    JOIN parent_student_links l ON l.parent_user_id=p.id AND l.status='active'
                    JOIN users s ON s.id=l.student_user_id AND s.account_category='student' AND s.is_active=true
                    JOIN enrollments e ON e.user_id=s.id AND e.status IN ('pending','approved')
                    JOIN clubs c ON c.id=e.club_id
                    JOIN teachers t ON t.id=c.teacher_id AND t.user_id=@teacher_user AND t.is_active=true
                    JOIN users tu ON tu.id=t.user_id AND tu.account_category='teacher' AND tu.is_active=true
                    WHERE p.id=@parent AND p.account_category='parent' AND p.is_active=true
                    AND s.id=@student AND t.id=@teacher", connection, transaction))
                {
                    check.Parameters.AddWithValue("parent", parentId);
                    check.Parameters.AddWithValue("student", studentId);
                    check.Parameters.AddWithValue("teacher", teacherId);
                    check.Parameters.AddWithValue("teacher_user", teacherUserId);
                    if (Convert.ToInt32(check.ExecuteScalar()) == 0) throw new InvalidOperationException("Чат доступен, если ученик связан с родителем и записан к этому преподавателю.");
                }
                using (var command = new NpgsqlCommand(@"INSERT INTO chat_conversations(student_user_id,teacher_id)
                    VALUES(@student,@teacher) ON CONFLICT(student_user_id,teacher_id) DO NOTHING", connection, transaction))
                {
                    command.Parameters.AddWithValue("student", studentId);
                    command.Parameters.AddWithValue("teacher", teacherId);
                    command.ExecuteNonQuery();
                }
                long id;
                using (var command = new NpgsqlCommand("SELECT id FROM chat_conversations WHERE student_user_id=@student AND teacher_id=@teacher", connection, transaction))
                {
                    command.Parameters.AddWithValue("student", studentId);
                    command.Parameters.AddWithValue("teacher", teacherId);
                    id = Convert.ToInt64(command.ExecuteScalar());
                }
                transaction.Commit();
                return id;
            }
        }

        // Получить диалог разрешено только родителю со связью active или назначенному преподавателю.
        public List<ChatMessageRow> Read(int userId, long conversationId, out ChatConversationRow header)
        {
            var messages = new List<ChatMessageRow>();
            header = null;
            using (var connection = Db.Open())
            {
                using (var command = new NpgsqlCommand(@"SELECT c.id,c.student_user_id,s.first_name||' '||s.last_name,t.first_name||' '||t.last_name
                    FROM chat_conversations c JOIN users s ON s.id=c.student_user_id JOIN teachers t ON t.id=c.teacher_id
                    WHERE c.id=@conversation AND (t.user_id=@user OR EXISTS(SELECT 1 FROM parent_student_links l
                    JOIN users p ON p.id=l.parent_user_id AND p.account_category='parent' AND p.is_active=true
                    JOIN users s ON s.id=l.student_user_id AND s.is_active=true
                    WHERE l.parent_user_id=@user AND l.student_user_id=c.student_user_id AND l.status='active'))", connection))
                {
                    command.Parameters.AddWithValue("conversation", conversationId);
                    command.Parameters.AddWithValue("user", userId);
                    using (var reader = command.ExecuteReader())
                        if (reader.Read()) header = new ChatConversationRow { Id=reader.GetInt64(0),StudentId=reader.GetInt32(1),StudentName=reader.GetString(2),TeacherName=reader.GetString(3) };
                }
                if (header == null) throw new UnauthorizedAccessException("У вас нет доступа к этому диалогу.");
                using (var command = new NpgsqlCommand(@"SELECT m.sender_user_id,u.first_name||' '||u.last_name,m.body,m.created_at
                    FROM chat_messages m JOIN users u ON u.id=m.sender_user_id
                    WHERE m.conversation_id=@conversation ORDER BY m.created_at,m.id", connection))
                {
                    command.Parameters.AddWithValue("conversation", conversationId);
                    using (var reader = command.ExecuteReader())
                        while (reader.Read()) messages.Add(new ChatMessageRow { IsMine=reader.GetInt32(0)==userId,SenderName=reader.GetString(1),Body=reader.GetString(2),CreatedAt=reader.GetDateTime(3) });
                }
            }
            return messages;
        }

        public void Send(int userId, long conversationId, string body)
        {
            body = (body ?? "").Trim();
            if (body.Length == 0 || body.Length > 4000) throw new ArgumentException("Сообщение должно содержать от 1 до 4000 символов.");
            using (var connection = Db.Open())
            using (var transaction = connection.BeginTransaction())
            {
                bool allowed;
                using (var command = new NpgsqlCommand(@"SELECT count(*) FROM chat_conversations c
                    JOIN users sender ON sender.id=@user AND sender.is_active=true
                    JOIN teachers t ON t.id=c.teacher_id
                    WHERE c.id=@conversation AND (
                        (sender.account_category='teacher' AND t.user_id=sender.id AND t.is_active=true)
                        OR (sender.account_category='parent' AND EXISTS(SELECT 1 FROM parent_student_links l
                            JOIN users student ON student.id=l.student_user_id AND student.is_active=true
                            WHERE l.parent_user_id=sender.id AND l.student_user_id=c.student_user_id AND l.status='active'))) ", connection, transaction))
                {
                    command.Parameters.AddWithValue("user", userId);
                    command.Parameters.AddWithValue("conversation", conversationId);
                    allowed = Convert.ToInt32(command.ExecuteScalar()) == 1;
                }
                if (!allowed) throw new UnauthorizedAccessException("Отправлять сообщения в этот диалог нельзя.");
                using (var command = new NpgsqlCommand("INSERT INTO chat_messages(conversation_id,sender_user_id,body) VALUES(@conversation,@sender,@body)", connection, transaction))
                {
                    command.Parameters.AddWithValue("conversation", conversationId);
                    command.Parameters.AddWithValue("sender", userId);
                    command.Parameters.AddWithValue("body", body);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        private int GetTeacherUserId(NpgsqlConnection connection, NpgsqlTransaction transaction, int teacherId)
        {
            using (var command = new NpgsqlCommand("SELECT COALESCE(user_id,0) FROM teachers WHERE id=@id", connection, transaction))
            { command.Parameters.AddWithValue("id", teacherId); return Convert.ToInt32(command.ExecuteScalar()); }
        }
    }
}
