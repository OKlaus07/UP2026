using ClubRegistration.Mvc.Models;
using Npgsql;
using System.Collections.Generic;

namespace ClubRegistration.Mvc.Services
{
    // Поиск кружков в каталоге и создание заявки от имени текущего пользователя.
    public class ClubService
    {
        public List<ClubCard> Search(string text, string category)
        {
            var clubs = new List<ClubCard>();
            using (var connection = Db.Open())
            using (var command = new NpgsqlCommand("SELECT c.id,c.name,ca.name,c.description,c.min_age,c.max_age,COALESCE(t.first_name||' '||t.last_name,'Преподаватель не назначен'),COALESCE(string_agg(CASE s.weekday WHEN 1 THEN 'Пн' WHEN 2 THEN 'Вт' WHEN 3 THEN 'Ср' WHEN 4 THEN 'Чт' WHEN 5 THEN 'Пт' WHEN 6 THEN 'Сб' ELSE 'Вс' END || ' ' || to_char(s.start_time,'HH24:MI'),' • '),'Расписание уточняется'),c.capacity-(SELECT count(*) FROM enrollments e WHERE e.club_id=c.id AND e.status='approved'),c.price FROM clubs c JOIN categories ca ON ca.id=c.category_id LEFT JOIN teachers t ON t.id=c.teacher_id LEFT JOIN schedules s ON s.club_id=c.id WHERE c.is_active AND c.name ILIKE @text AND (@category='' OR ca.name=@category) GROUP BY c.id,ca.name,t.first_name,t.last_name ORDER BY c.name", connection))
            {
                command.Parameters.AddWithValue("text", "%" + (text ?? "") + "%");
                command.Parameters.AddWithValue("category", category ?? "");
                using (var reader = command.ExecuteReader()) while (reader.Read()) clubs.Add(new ClubCard { Id=reader.GetInt32(0),Name=reader.GetString(1),Category=reader.GetString(2),Description=reader.GetString(3),AgeFrom=reader.GetInt32(4),AgeTo=reader.GetInt32(5),Teacher=reader.GetString(6),Schedule=reader.GetString(7),FreePlaces=reader.GetInt32(8),Price=reader.GetDecimal(9) });
            }
            return clubs;
        }

        public List<string> Categories()
        {
            var items = new List<string>();
            using (var connection=Db.Open()) using (var command=new NpgsqlCommand("SELECT name FROM categories ORDER BY name",connection)) using (var reader=command.ExecuteReader()) while(reader.Read()) items.Add(reader.GetString(0));
            return items;
        }

        public string Apply(int userId,int clubId)
        {
            using(var connection=Db.Open()) using(var command=new NpgsqlCommand("INSERT INTO enrollments(user_id,club_id,status) SELECT @user,@club,'pending' WHERE EXISTS(SELECT 1 FROM clubs WHERE id=@club AND is_active) AND NOT EXISTS(SELECT 1 FROM enrollments WHERE user_id=@user AND club_id=@club AND status IN ('pending','approved'))",connection))
            {
                command.Parameters.AddWithValue("user",userId); command.Parameters.AddWithValue("club",clubId);
                try { return command.ExecuteNonQuery()==1 ? null : "Новая заявка не создана: проверьте направление или активную заявку."; }
                catch(PostgresException error) when(error.SqlState=="23505") { return "У вас уже есть активная заявка на это направление."; }
            }
        }
    }
}
