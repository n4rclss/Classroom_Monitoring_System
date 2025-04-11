using App.Server.Database.Models;
using App.Server.Database;
using System;
using System.Collections.Generic;
using System.Data;
using App.Server.Helper;

namespace App.Server.Database.Controller
{
    public class Students : IController<Student>
    {
        private readonly DbHelper _dbHelper;

        public Students(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public IEnumerable<Student> GetAll()
        {
            string query = "SELECT StudentId, FullName, RoomId FROM Students";
            DataTable dataTable = _dbHelper.ExecuteQuery(query);

            List<Student> students = new List<Student>();
            foreach (DataRow row in dataTable.Rows)
            {
                students.Add(new Student
                {
                    StudentId = Convert.ToInt32(row["StudentId"]),
                    FullName = row["FullName"].ToString(),
                    RoomId = row["RoomId"] != DBNull.Value ? Convert.ToInt32(row["RoomId"]) : (int?)null
                });
            }

            return students;
        }

        public bool IdExists(int id)
        {
            string query = "SELECT COUNT(*) FROM Students WHERE StudentId = @StudentId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@StudentId", id }
            };
            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);
            return dataTable.Rows.Count > 0 && Convert.ToInt32(dataTable.Rows[0][0]) > 0;
        }
        public Student GetById(int id)
        {
            string query = "SELECT StudentId, FullName, RoomId FROM Students WHERE StudentId = @StudentId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@StudentId", id }
            };

            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            DataRow row = dataTable.Rows[0];
            return new Student
            {
                StudentId = Convert.ToInt32(row["StudentId"]),
                FullName = row["FullName"].ToString(),
                RoomId = row["RoomId"] != DBNull.Value ? Convert.ToInt32(row["RoomId"]) : (int?)null
            };
        }

        public bool Insert(Student student)
        {
            string query = "INSERT INTO Students (StudentId, FullName, Password, RoomId) VALUES (@StudentId, @FullName, @Password, @RoomId)";
            if (IdExists(student.StudentId))
                return false;
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@StudentId", student.StudentId },
                { "@FullName", student.FullName },
                { "@Password", PasswordHelper.HashPassword(student.Password) },
                { "@RoomId", student.RoomId.HasValue ? (object)student.RoomId.Value : DBNull.Value }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Update(Student student)
        {
            string query = "UPDATE Students SET FullName = @FullName, RoomId = @RoomId WHERE StudentId = @StudentId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@FullName", student.FullName },
                { "@RoomId", student.RoomId.HasValue ? (object)student.RoomId.Value : DBNull.Value },
                { "@StudentId", student.StudentId }

            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Delete(int id)
        {
            string query = "DELETE FROM Students WHERE StudentId = @StudentId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@StudentId", id }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Authenticate(int userID, string password)
        {

            string query = "SELECT Password FROM Students WHERE StudentId = @UserId";
            var parameters = new Dictionary<string, object>
            {
                { "@UserId", userID }
            };

            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);

            if (dataTable.Rows.Count == 0)
                return false;

            string storedHash = dataTable.Rows[0]["Password"].ToString();
            return PasswordHelper.VerifyPassword(password, storedHash);
        }
    }
}