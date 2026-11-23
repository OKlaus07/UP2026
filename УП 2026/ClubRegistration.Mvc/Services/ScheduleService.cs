using ClubRegistration.Mvc.Models;
using Npgsql;
using System.Collections.Generic;
namespace ClubRegistration.Mvc.Services
{
    // Чтение расписания активных кружков для страницы сайта.
    public class ScheduleService
    {
        public List<ScheduleRow> All()
        {
            var rows=new List<ScheduleRow>();
            const string sql="SELECT c.name,CASE s.weekday WHEN 1 THEN 'Понедельник' WHEN 2 THEN 'Вторник' WHEN 3 THEN 'Среда' WHEN 4 THEN 'Четверг' WHEN 5 THEN 'Пятница' WHEN 6 THEN 'Суббота' ELSE 'Воскресенье' END,to_char(s.start_time,'HH24:MI'),to_char(s.end_time,'HH24:MI'),t.first_name||' '||t.last_name,s.room FROM schedules s JOIN clubs c ON c.id=s.club_id JOIN teachers t ON t.id=s.teacher_id JOIN clubs active ON active.id=s.club_id AND active.is_active ORDER BY s.weekday,s.start_time";
            using(var connection=Db.Open())using(var command=new NpgsqlCommand(sql,connection))using(var reader=command.ExecuteReader())while(reader.Read())rows.Add(new ScheduleRow{ClubName=reader.GetString(0),Day=reader.GetString(1),Start=reader.GetString(2),End=reader.GetString(3),Teacher=reader.GetString(4),Room=reader.GetString(5)});
            return rows;
        }
    }
}
