using ClubRegistration.Mvc.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace ClubRegistration.Mvc.Services
{
    // Все SQL-операции панели администратора: кружки, преподаватели, расписание и пользователи.
    public class AdminService
    {
        public AdminDashboard Load(int? editClub,int? editTeacher,int? editSchedule)
        {
            var result=new AdminDashboard{Clubs=new List<AdminClubRow>(),Teachers=new List<AdminTeacherRow>(),Schedules=new List<AdminScheduleRow>(),Users=new List<AdminUserRow>(),Categories=CategoryNames(),CategoryRows=LoadCategoryRows()};
            using(var connection=Db.Open())
            {
                using(var command=new NpgsqlCommand("SELECT c.id,c.name,c.category_id,ca.name,c.description,c.min_age,c.max_age,c.price,c.capacity,COALESCE(c.teacher_id,0),COALESCE(t.first_name||' '||t.last_name,''),c.is_active FROM clubs c JOIN categories ca ON ca.id=c.category_id LEFT JOIN teachers t ON t.id=c.teacher_id ORDER BY c.name",connection))using(var reader=command.ExecuteReader())while(reader.Read()){var row=new AdminClubRow{Id=reader.GetInt32(0),Name=reader.GetString(1),CategoryId=reader.GetInt32(2),Category=reader.GetString(3),Description=reader.GetString(4),MinAge=reader.GetInt32(5),MaxAge=reader.GetInt32(6),Price=reader.GetDecimal(7),Capacity=reader.GetInt32(8),TeacherId=reader.GetInt32(9),Teacher=reader.GetString(10),Active=reader.GetBoolean(11)};result.Clubs.Add(row);if(editClub==row.Id)result.EditClub=row;}
                using(var command=new NpgsqlCommand("SELECT id,first_name,last_name,COALESCE(middle_name,''),specialization,COALESCE(phone,''),COALESCE(description,''),is_active,COALESCE(user_id,0) FROM teachers ORDER BY last_name",connection))using(var reader=command.ExecuteReader())while(reader.Read()){var row=new AdminTeacherRow{Id=reader.GetInt32(0),FirstName=reader.GetString(1),LastName=reader.GetString(2),MiddleName=reader.GetString(3),Specialty=reader.GetString(4),Phone=reader.GetString(5),Description=reader.GetString(6),Active=reader.GetBoolean(7),UserId=reader.GetInt32(8)};result.Teachers.Add(row);if(editTeacher==row.Id)result.EditTeacher=row;}
                using(var command=new NpgsqlCommand("SELECT s.id,s.club_id,c.name,s.teacher_id,t.first_name||' '||t.last_name,s.weekday,to_char(s.start_time,'HH24:MI'),to_char(s.end_time,'HH24:MI'),s.room FROM schedules s JOIN clubs c ON c.id=s.club_id JOIN teachers t ON t.id=s.teacher_id ORDER BY s.weekday,s.start_time",connection))using(var reader=command.ExecuteReader())while(reader.Read()){var row=new AdminScheduleRow{Id=reader.GetInt32(0),ClubId=reader.GetInt32(1),Club=reader.GetString(2),TeacherId=reader.GetInt32(3),Teacher=reader.GetString(4),Weekday=reader.GetInt32(5),Day=DayName(reader.GetInt32(5)),Start=reader.GetString(6),End=reader.GetString(7),Room=reader.GetString(8)};result.Schedules.Add(row);if(editSchedule==row.Id)result.EditSchedule=row;}
                using(var command=new NpgsqlCommand("SELECT id,first_name,last_name,email,COALESCE(phone,''),account_category,role,is_active FROM users ORDER BY last_name",connection))using(var reader=command.ExecuteReader())while(reader.Read())result.Users.Add(new AdminUserRow{Id=reader.GetInt32(0),FirstName=reader.GetString(1),LastName=reader.GetString(2),Email=reader.GetString(3),Phone=reader.GetString(4),CategoryCode=reader.GetString(5),AccountCategory=AccountCategoryName(reader.GetString(5)),Role=reader.GetString(6),Active=reader.GetBoolean(7)});
                using(var command=new NpgsqlCommand("SELECT (SELECT count(*) FROM users),(SELECT count(*) FROM clubs),(SELECT count(*) FROM teachers),(SELECT count(*) FROM enrollments),(SELECT count(*) FROM enrollments WHERE status='approved'),(SELECT count(*) FROM enrollments WHERE status='pending')",connection))using(var reader=command.ExecuteReader()){reader.Read();result.Statistics="Пользователей: "+reader.GetInt64(0)+" · Кружков: "+reader.GetInt64(1)+" · Преподавателей: "+reader.GetInt64(2)+" · Заявок: "+reader.GetInt64(3)+" · Одобрено: "+reader.GetInt64(4)+" · Ожидают: "+reader.GetInt64(5);}
                using(var command=new NpgsqlCommand("SELECT c.name,count(e.id) FROM clubs c LEFT JOIN enrollments e ON e.club_id=c.id GROUP BY c.id,c.name ORDER BY count(e.id) DESC,c.name LIMIT 3",connection))using(var reader=command.ExecuteReader()){var names=new List<string>();while(reader.Read())names.Add(reader.GetString(0)+" ("+reader.GetInt64(1)+")");result.Popular=names.Count==0?"Пока нет данных":string.Join(", ",names);}
            }
            return result;
        }
        public List<string> CategoryNames(){var list=new List<string>();using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT name FROM categories ORDER BY name",connection))using(var reader=command.ExecuteReader())while(reader.Read())list.Add(reader.GetString(0));return list;}
        public List<KeyValuePair<int,string>> Categories(){var list=new List<KeyValuePair<int,string>>();using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT id,name FROM categories ORDER BY name",connection))using(var reader=command.ExecuteReader())while(reader.Read())list.Add(new KeyValuePair<int,string>(reader.GetInt32(0),reader.GetString(1)));return list;}
        // Добавляет категорию или меняет её название, если передан её идентификатор.
        public void SaveCategory(int id,string name)
        {
            if(id<0)throw new ArgumentException("Некорректный номер категории.");
            var validationError=BusinessRules.ValidateCategory(name);
            if(validationError!=null)throw new ArgumentException(validationError);
            name=name.Trim();
            string sql=id==0?"INSERT INTO categories(name) VALUES(@name)":"UPDATE categories SET name=@name WHERE id=@id";
            using(var connection=Db.Open())using(var command=new NpgsqlCommand(sql,connection))
            {
                command.Parameters.AddWithValue("name",name);
                if(id>0)command.Parameters.AddWithValue("id",id);
                try{if(command.ExecuteNonQuery()!=1)throw new ArgumentException("Категория не найдена.");}
                catch(PostgresException error)when(error.SqlState=="23505"){throw new ArgumentException("Категория с таким названием уже есть.");}
            }
        }

        // Категорию можно удалить, только если на неё не ссылаются кружки.
        public bool DeleteCategory(int id)
        {
            using(var connection=Db.Open())using(var command=new NpgsqlCommand("DELETE FROM categories WHERE id=@id AND NOT EXISTS(SELECT 1 FROM clubs WHERE category_id=@id)",connection))
            {command.Parameters.AddWithValue("id",id);return command.ExecuteNonQuery()==1;}
        }

        private List<AdminCategoryRow> LoadCategoryRows()
        {
            var rows=new List<AdminCategoryRow>();
            using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT ca.id,ca.name,count(c.id) FROM categories ca LEFT JOIN clubs c ON c.category_id=ca.id GROUP BY ca.id,ca.name ORDER BY ca.name",connection))using(var reader=command.ExecuteReader())
                while(reader.Read())rows.Add(new AdminCategoryRow{Id=reader.GetInt32(0),Name=reader.GetString(1),ClubsCount=reader.GetInt64(2)>int.MaxValue?int.MaxValue:(int)reader.GetInt64(2)});
            return rows;
        }
        public void SaveClub(AdminClubRow x){if(x==null)throw new ArgumentException("Заполните данные кружка.");if(x.Id<0)throw new ArgumentException("Некорректный номер кружка.");var error=BusinessRules.ValidateClub(x.Name,x.MinAge,x.MaxAge,x.Price,x.Capacity);if(error!=null)throw new ArgumentException(error);if(x.Name.Trim().Length>150)throw new ArgumentException("Название кружка не должно превышать 150 символов.");string sql=x.Id==0?"INSERT INTO clubs(name,category_id,description,min_age,max_age,price,capacity,teacher_id) VALUES(@name,@category,@description,@min,@max,@price,@capacity,@teacher)":"UPDATE clubs SET name=@name,category_id=@category,description=@description,min_age=@min,max_age=@max,price=@price,capacity=@capacity,teacher_id=@teacher WHERE id=@id AND @capacity >= (SELECT count(*) FROM enrollments WHERE club_id=@id AND status='approved')";using(var connection=Db.Open())using(var command=new NpgsqlCommand(sql,connection)){ClubParameters(command,x);if(x.Id>0)command.Parameters.AddWithValue("id",x.Id);if(command.ExecuteNonQuery()==0)throw new ArgumentException("Кружок не найден или вместимость меньше числа одобренных заявок.");}}
        public bool DeactivateClub(int id){return Run("UPDATE clubs SET is_active=false WHERE id=@id",id);}
        public bool ActivateClub(int id){return Run("UPDATE clubs SET is_active=true WHERE id=@id",id);}
        public bool DeleteClub(int id){using(var connection=Db.Open())using(var command=new NpgsqlCommand("DELETE FROM clubs WHERE id=@id AND NOT EXISTS(SELECT 1 FROM enrollments WHERE club_id=@id)",connection)){command.Parameters.AddWithValue("id",id);return command.ExecuteNonQuery()==1;}}
        public void SaveTeacher(AdminTeacherRow x)
        {
            if(x==null||x.Id<0||x.UserId<0)throw new ArgumentException("Некорректные данные преподавателя.");
            if(string.IsNullOrWhiteSpace(x.FirstName)||string.IsNullOrWhiteSpace(x.LastName)||string.IsNullOrWhiteSpace(x.Specialty))throw new ArgumentException("Имя, фамилия и специализация обязательны.");
            if(x.FirstName.Trim().Length>80||x.LastName.Trim().Length>80||(x.MiddleName??"").Trim().Length>80||x.Specialty.Trim().Length>150||(x.Phone??"").Trim().Length>30)throw new ArgumentException("Проверьте длину имени, специализации и телефона.");
            string sql=x.Id==0?"INSERT INTO teachers(first_name,last_name,middle_name,specialization,phone,description,user_id) VALUES(@first,@last,@middle,@specialty,@phone,@description,@user)":"UPDATE teachers SET first_name=@first,last_name=@last,middle_name=@middle,specialization=@specialty,phone=@phone,description=@description,user_id=@user WHERE id=@id";
            using(var connection=Db.Open())
            {
                if(x.UserId>0)using(var account=new NpgsqlCommand("SELECT count(*) FROM users WHERE id=@id AND account_category='teacher' AND is_active=true",connection)){account.Parameters.AddWithValue("id",x.UserId);if(Convert.ToInt64(account.ExecuteScalar())!=1)throw new ArgumentException("Для связи выберите активный аккаунт категории «Преподаватель».");}
                using(var command=new NpgsqlCommand(sql,connection))
                {
                    TeacherParameters(command,x);if(x.Id>0)command.Parameters.AddWithValue("id",x.Id);
                    try{if(command.ExecuteNonQuery()!=1)throw new ArgumentException("Преподаватель не найден.");}
                    catch(PostgresException error)when(error.SqlState=="23505"){throw new ArgumentException("Этот аккаунт уже связан с другим преподавателем.");}
                }
            }
        }
        public List<AdminAccountChoice> TeacherAccounts(){var list=new List<AdminAccountChoice>();using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT id,first_name||' '||last_name||' · '||email FROM users WHERE account_category='teacher' AND is_active=true ORDER BY last_name,first_name",connection))using(var reader=command.ExecuteReader())while(reader.Read())list.Add(new AdminAccountChoice{Id=reader.GetInt32(0),Name=reader.GetString(1)});return list;}
        public bool DeactivateTeacher(int id){return Run("UPDATE teachers SET is_active=false WHERE id=@id",id);}
        public bool ActivateTeacher(int id){return Run("UPDATE teachers SET is_active=true WHERE id=@id",id);}
        public void SaveSchedule(AdminScheduleRow x){if(x==null||x.Id<0)throw new ArgumentException("Некорректные данные занятия.");TimeSpan start,end;if(!TimeSpan.TryParse(x.Start,out start)||!TimeSpan.TryParse(x.End,out end)||!BusinessRules.IsScheduleValid(x.Weekday,start,end)||string.IsNullOrWhiteSpace(x.Room)||x.Room.Trim().Length>80)throw new ArgumentException("Проверьте день, время и кабинет (не более 80 символов).");using(var connection=Db.Open())using(var command=new NpgsqlCommand("SELECT count(*) FROM schedules WHERE id<>@id AND weekday=@day AND start_time<@end AND end_time>@start AND (teacher_id=@teacher OR lower(room)=lower(@room))",connection)){command.Parameters.AddWithValue("id",x.Id);command.Parameters.AddWithValue("day",x.Weekday);command.Parameters.AddWithValue("end",end);command.Parameters.AddWithValue("start",start);command.Parameters.AddWithValue("teacher",x.TeacherId);command.Parameters.AddWithValue("room",x.Room);if(Convert.ToInt64(command.ExecuteScalar())>0)throw new ArgumentException("Преподаватель или кабинет занят.");}string sql=x.Id==0?"INSERT INTO schedules(club_id,teacher_id,weekday,start_time,end_time,room) VALUES(@club,@teacher,@day,@start,@end,@room)":"UPDATE schedules SET club_id=@club,teacher_id=@teacher,weekday=@day,start_time=@start,end_time=@end,room=@room WHERE id=@id";using(var connection=Db.Open())using(var command=new NpgsqlCommand(sql,connection)){ScheduleParameters(command,x,start,end);if(x.Id>0)command.Parameters.AddWithValue("id",x.Id);if(command.ExecuteNonQuery()!=1)throw new ArgumentException("Занятие расписания не найдено.");}}
        public bool DeleteSchedule(int id){return Run("DELETE FROM schedules WHERE id=@id",id);}
        public bool DeactivateUser(int id){return Run("UPDATE users SET is_active=false WHERE id=@id AND role<>'admin'",id);}
        public bool ActivateUser(int id){return Run("UPDATE users SET is_active=true WHERE id=@id AND role<>'admin'",id);}
        // Администратор может создавать учётные записи; пароль сразу хешируется с солью.
        public void CreateUser(AdminCreateUserModel user)
        {
            if(user==null||string.IsNullOrWhiteSpace(user.FirstName)||string.IsNullOrWhiteSpace(user.LastName)||string.IsNullOrWhiteSpace(user.Email))throw new ArgumentException("Заполните имя, фамилию и почту.");
            if(user.Role!="user"&&user.Role!="admin")throw new ArgumentException("Недопустимая роль аккаунта.");
            if(!IsAccountCategory(user.AccountCategory))throw new ArgumentException("Выберите допустимую категорию аккаунта.");
            if(string.IsNullOrWhiteSpace(user.Password)||user.Password.Length<8)throw new ArgumentException("Пароль должен содержать не менее 8 символов.");
            var salt=PasswordService.NewSalt();
            using(var connection=Db.Open())using(var command=new NpgsqlCommand("INSERT INTO users(first_name,last_name,email,phone,password_hash,password_salt,role,account_category) VALUES(@first,@last,@email,@phone,@hash,@salt,@role,@category)",connection))
            {
                command.Parameters.AddWithValue("first",user.FirstName.Trim());command.Parameters.AddWithValue("last",user.LastName.Trim());command.Parameters.AddWithValue("email",user.Email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("phone",string.IsNullOrWhiteSpace(user.Phone)?(object)DBNull.Value:user.Phone.Trim());command.Parameters.AddWithValue("hash",PasswordService.Hash(user.Password,salt));command.Parameters.AddWithValue("salt",salt);command.Parameters.AddWithValue("role",user.Role);command.Parameters.AddWithValue("category",user.AccountCategory);
                try{command.ExecuteNonQuery();}catch(PostgresException error)when(error.SqlState=="23505"){throw new ArgumentException("Пользователь с такой почтой уже существует.");}
            }
        }
        // Изменяет личные данные и тип аккаунта, не меняя его роль доступа.
        public void UpdateUser(AdminEditUserModel user)
        {
            if(user==null||user.Id<=0||string.IsNullOrWhiteSpace(user.FirstName)||string.IsNullOrWhiteSpace(user.LastName)||string.IsNullOrWhiteSpace(user.Email))throw new ArgumentException("Проверьте личные данные пользователя.");
            if(!IsAccountCategory(user.AccountCategory))throw new ArgumentException("Выберите допустимую категорию аккаунта.");
            using(var connection=Db.Open())
            {
                using(var check=new NpgsqlCommand("SELECT count(*) FROM teachers WHERE user_id=@id AND @category<>'teacher'",connection)){check.Parameters.AddWithValue("id",user.Id);check.Parameters.AddWithValue("category",user.AccountCategory);if(Convert.ToInt64(check.ExecuteScalar())>0)throw new ArgumentException("Сначала отвяжите аккаунт от карточки преподавателя.");}
                using(var check=new NpgsqlCommand("SELECT count(*) FROM parent_student_links WHERE status IN ('pending','active') AND ((parent_user_id=@id AND @category<>'parent') OR (student_user_id=@id AND @category<>'student'))",connection)){check.Parameters.AddWithValue("id",user.Id);check.Parameters.AddWithValue("category",user.AccountCategory);if(Convert.ToInt64(check.ExecuteScalar())>0)throw new ArgumentException("Сначала удалите семейные связи, затем меняйте категорию аккаунта.");}
                using(var command=new NpgsqlCommand("UPDATE users SET first_name=@first,last_name=@last,email=@email,phone=@phone,account_category=@category WHERE id=@id",connection))
                {
                command.Parameters.AddWithValue("first",user.FirstName.Trim());command.Parameters.AddWithValue("last",user.LastName.Trim());command.Parameters.AddWithValue("email",user.Email.Trim().ToLowerInvariant());command.Parameters.AddWithValue("phone",string.IsNullOrWhiteSpace(user.Phone)?(object)DBNull.Value:user.Phone.Trim());command.Parameters.AddWithValue("category",user.AccountCategory);command.Parameters.AddWithValue("id",user.Id);
                try{if(command.ExecuteNonQuery()!=1)throw new ArgumentException("Пользователь не найден.");}catch(PostgresException error)when(error.SqlState=="23505"){throw new ArgumentException("Пользователь с такой почтой уже существует.");}
                }
            }
        }
        // Не позволяет случайно снять роль у последнего администратора.
        public bool SetUserRole(int id,string role)
        {
            if(role!="user"&&role!="admin")throw new ArgumentException("Недопустимая роль аккаунта.");
            using(var connection=Db.Open())using(var transaction=connection.BeginTransaction())
            {
                using(var guard=new NpgsqlCommand("SELECT pg_advisory_xact_lock(984311)",connection,transaction))guard.ExecuteNonQuery();
                if(role=="user")using(var count=new NpgsqlCommand("SELECT count(*) FROM users WHERE role='admin' AND is_active=true",connection,transaction))if(Convert.ToInt64(count.ExecuteScalar())<=1&&IsLastAdmin(connection,transaction,id))return false;
                using(var update=new NpgsqlCommand("UPDATE users SET role=@role WHERE id=@id AND (role<>'admin' OR @role='admin' OR EXISTS(SELECT 1 FROM users WHERE role='admin' AND is_active=true AND id<>@id))",connection,transaction)){update.Parameters.AddWithValue("role",role);update.Parameters.AddWithValue("id",id);var changed=update.ExecuteNonQuery()==1;transaction.Commit();return changed;}
            }
        }
        private bool IsLastAdmin(NpgsqlConnection connection,NpgsqlTransaction transaction,int id){using(var command=new NpgsqlCommand("SELECT count(*) FROM users WHERE id=@id AND role='admin' AND is_active=true",connection,transaction)){command.Parameters.AddWithValue("id",id);return Convert.ToInt64(command.ExecuteScalar())==1;}}
        // Историю заявок сохраняем: удаление разрешено только аккаунтам без заявок.
        public bool DeleteUser(int id){using(var connection=Db.Open())using(var command=new NpgsqlCommand("DELETE FROM users WHERE id=@id AND role<>'admin' AND NOT EXISTS(SELECT 1 FROM enrollments WHERE user_id=@id)",connection)){command.Parameters.AddWithValue("id",id);return command.ExecuteNonQuery()==1;}}
        private bool IsAccountCategory(string value){return value=="parent"||value=="student"||value=="college_student"||value=="teacher";}
        private string AccountCategoryName(string value){switch(value){case "parent":return "Родитель";case "student":return "Ученик";case "college_student":return "Студент";case "teacher":return "Преподаватель";default:return value;}}
        private void ClubParameters(NpgsqlCommand c,AdminClubRow x){c.Parameters.AddWithValue("name",x.Name.Trim());c.Parameters.AddWithValue("category",x.CategoryId);c.Parameters.AddWithValue("description",x.Description??"");c.Parameters.AddWithValue("min",x.MinAge);c.Parameters.AddWithValue("max",x.MaxAge);c.Parameters.AddWithValue("price",x.Price);c.Parameters.AddWithValue("capacity",x.Capacity);c.Parameters.AddWithValue("teacher",x.TeacherId==0?(object)DBNull.Value:x.TeacherId);}
        private void TeacherParameters(NpgsqlCommand c,AdminTeacherRow x){c.Parameters.AddWithValue("first",x.FirstName.Trim());c.Parameters.AddWithValue("last",x.LastName.Trim());c.Parameters.AddWithValue("middle",string.IsNullOrWhiteSpace(x.MiddleName)?(object)DBNull.Value:x.MiddleName.Trim());c.Parameters.AddWithValue("specialty",x.Specialty.Trim());c.Parameters.AddWithValue("phone",string.IsNullOrWhiteSpace(x.Phone)?(object)DBNull.Value:x.Phone);c.Parameters.AddWithValue("description",(object)x.Description??DBNull.Value);c.Parameters.AddWithValue("user",x.UserId==0?(object)DBNull.Value:x.UserId);}
        private void ScheduleParameters(NpgsqlCommand c,AdminScheduleRow x,TimeSpan start,TimeSpan end){c.Parameters.AddWithValue("club",x.ClubId);c.Parameters.AddWithValue("teacher",x.TeacherId);c.Parameters.AddWithValue("day",x.Weekday);c.Parameters.AddWithValue("start",start);c.Parameters.AddWithValue("end",end);c.Parameters.AddWithValue("room",x.Room.Trim());}
        private bool Run(string sql,int id){using(var connection=Db.Open())using(var command=new NpgsqlCommand(sql,connection)){command.Parameters.AddWithValue("id",id);return command.ExecuteNonQuery()==1;}}
        private string DayName(int n){return new[]{"","Понедельник","Вторник","Среда","Четверг","Пятница","Суббота","Воскресенье"}[n];}
    }
}
