using ClubRegistration.Mvc.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace ClubRegistration.Mvc.Services
{
    // Просмотр и безопасная обработка заявок. Решения администратора пишутся в уведомления и журнал.
    public class EnrollmentService
    {
        public List<EnrollmentRow> Mine(int userId) { return Load("WHERE e.user_id=@id",userId); }
        public List<EnrollmentRow> All() { return Load("",null); }
        private List<EnrollmentRow> Load(string filter,int? id)
        {
            var rows=new List<EnrollmentRow>();
            using(var connection=Db.Open()) using(var command=new NpgsqlCommand("SELECT e.id,e.user_id,u.first_name||' '||u.last_name,u.email,c.name,e.created_at,e.status FROM enrollments e JOIN users u ON u.id=e.user_id JOIN clubs c ON c.id=e.club_id "+filter+" ORDER BY e.created_at DESC",connection))
            {
                if(id.HasValue)command.Parameters.AddWithValue("id",id.Value);
                using(var reader=command.ExecuteReader())while(reader.Read())rows.Add(new EnrollmentRow{Id=reader.GetInt32(0),UserId=reader.GetInt32(1),UserName=reader.GetString(2),Email=reader.GetString(3),ClubName=reader.GetString(4),CreatedAt=reader.GetDateTime(5),Status=RussianStatus(reader.GetString(6))});
            }
            return rows;
        }
        public string Cancel(int id,int userId)
        {
            using(var connection=Db.Open())using(var command=new NpgsqlCommand("UPDATE enrollments SET status='cancelled' WHERE id=@id AND user_id=@user AND status='pending'",connection))
            {command.Parameters.AddWithValue("id",id);command.Parameters.AddWithValue("user",userId);return command.ExecuteNonQuery()==1?null:"Можно отменить только заявку, ожидающую рассмотрения.";}
        }
        // Родитель может отменить только ожидающую заявку ученика с подтверждённой связью.
        public string CancelForParent(int enrollmentId,int parentId)
        {
            using(var connection=Db.Open())using(var command=new NpgsqlCommand(@"UPDATE enrollments e SET status='cancelled'
                WHERE e.id=@enrollment AND e.status='pending' AND EXISTS(
                    SELECT 1 FROM parent_student_links l JOIN users s ON s.id=l.student_user_id AND s.is_active=true WHERE l.parent_user_id=@parent
                    AND l.student_user_id=e.user_id AND l.status='active')",connection))
            {command.Parameters.AddWithValue("enrollment",enrollmentId);command.Parameters.AddWithValue("parent",parentId);return command.ExecuteNonQuery()==1?null:"Можно отменить только ожидающую заявку ребёнка при активной семейной связи.";}
        }
        public string SetStatus(int id,string newStatus,int adminId)
        {
            if(newStatus!="approved"&&newStatus!="rejected")return "Недопустимый статус.";
            using(var connection=Db.Open())using(var tx=connection.BeginTransaction())try
            {
                int clubId;int userId;string clubName;string oldStatus;
                using(var cmd=new NpgsqlCommand("SELECT e.club_id,e.status,e.user_id,c.name FROM enrollments e JOIN clubs c ON c.id=e.club_id WHERE e.id=@id FOR UPDATE OF e",connection,tx)){cmd.Parameters.AddWithValue("id",id);using(var reader=cmd.ExecuteReader()){if(!reader.Read())return "Заявка не найдена.";clubId=reader.GetInt32(0);oldStatus=reader.GetString(1);userId=reader.GetInt32(2);clubName=reader.GetString(3);}}
                if(oldStatus!="pending")return "Можно рассматривать только ожидающие заявки.";
                if(newStatus=="approved")
                {
                    int capacity;using(var cmd=new NpgsqlCommand("SELECT capacity FROM clubs WHERE id=@id FOR UPDATE",connection,tx)){cmd.Parameters.AddWithValue("id",clubId);capacity=Convert.ToInt32(cmd.ExecuteScalar());}
                    int approved;using(var cmd=new NpgsqlCommand("SELECT count(*) FROM enrollments WHERE club_id=@id AND status='approved'",connection,tx)){cmd.Parameters.AddWithValue("id",clubId);approved=Convert.ToInt32(cmd.ExecuteScalar());}
                    if(!BusinessRules.CanApprove(oldStatus,approved,capacity))return "Свободных мест нет.";
                }
                using(var cmd=new NpgsqlCommand("UPDATE enrollments SET status=@status WHERE id=@id",connection,tx)){cmd.Parameters.AddWithValue("status",newStatus);cmd.Parameters.AddWithValue("id",id);cmd.ExecuteNonQuery();}
                string statusText = newStatus == "approved" ? "одобрена" : "отклонена";
                using(var cmd=new NpgsqlCommand("INSERT INTO notifications(user_id,message) VALUES(@user,@message)",connection,tx)){cmd.Parameters.AddWithValue("user",userId);cmd.Parameters.AddWithValue("message","Заявка на кружок «"+clubName+"» "+statusText+".");cmd.ExecuteNonQuery();}
                using(var cmd=new NpgsqlCommand("INSERT INTO audit_log(actor_user_id,action,entity_type,entity_id,details) VALUES(@actor,'enrollment_status_changed','enrollment',@id,@details)",connection,tx)){cmd.Parameters.AddWithValue("actor",adminId);cmd.Parameters.AddWithValue("id",id);cmd.Parameters.AddWithValue("details",statusText);cmd.ExecuteNonQuery();}
                tx.Commit();return null;
            }
            catch{tx.Rollback();throw;}
        }
        private string RussianStatus(string value){switch(value){case "pending":return "Ожидает рассмотрения";case "approved":return "Одобрена";case "rejected":return "Отклонена";default:return "Отменена";}}
    }
}
