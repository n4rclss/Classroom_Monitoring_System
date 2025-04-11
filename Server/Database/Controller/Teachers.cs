using App.Server.Database.Models;
using App.Server.Database;
using System;
using System.Collections.Generic;
using System.Data;
using App.Server.Helper;

namespace App.Server.Database.Controller
{
    public class Teachers : IController<Teacher>
    {
        private readonly DbHelper _dbHelper;

        public Teachers(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public IEnumerable<Teacher> GetAll()
        {
            string query = "SELECT TeacherId, FullName FROM Teachers";
            DataTable dataTable = _dbHelper.ExecuteQuery(query);

            List<Teacher> teachers = new List<Teacher>();
            foreach (DataRow row in dataTable.Rows)
            {
                teachers.Add(new Teacher
                {
                    TeacherId = Convert.ToInt32(row["TeacherId"]),
                    FullName = row["FullName"].ToString(),
                });
            }

            return teachers;
        }

        public bool IdExists(int id)
        {
            string query = "SELECT COUNT(*) FROM Teachers WHERE TeacherId = @TeacherId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
                {
                { "@TeacherId", id }
            };
            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);
            return dataTable.Rows.Count > 0 && Convert.ToInt32(dataTable.Rows[0][0]) > 0;
        }
        
        public Teacher GetById(int id)
        {
            string query = "SELECT TeacherId, FullName FROM Teachers WHERE TeacherId=@TeacherId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@TeacherId", id }
            };

            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            DataRow row = dataTable.Rows[0];
            return new Teacher
            {
                TeacherId = Convert.ToInt32(row["TeacherId"]),
                FullName = row["FullName"].ToString(),
            };
        }

        public bool Insert(Teacher teacher)
        {
            string query = "INSERT INTO Teachers (TeacherId, FullName, Password) VALUES (@TeacherId, @FullName, @Password)";
            if (IdExists(teacher.TeacherId))
                return false;
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@TeacherId", teacher.TeacherId },
                { "@FullName", teacher.FullName },
                { "@Password", PasswordHelper.HashPassword(teacher.Password) }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Update(Teacher teacher)
        {
            string query = "UPDATE Teachers SET FullName =@FullName, WHERE TeacherId=@TeacherId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@FullName", teacher.FullName },
                { "@TeacherId", teacher.TeacherId }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Delete(int id)
        {
            string query = "DELETE FROM Teachers WHERE TeacherId = @TeacherId";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@TeacherId", id }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Authenticate(int userID, string password)
        {

            string query = "SELECT Password FROM Teachers WHERE TeacherId = @UserId";
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



    