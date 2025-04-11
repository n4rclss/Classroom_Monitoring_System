using App.Server.Database.Models;
using App.Server.Database;
using App.Server.Helper;
using System;
using System.Collections.Generic;
using System.Data;

namespace App.Server.Database.Controller
{
    public class Rooms : IController<Room>
    {
        private readonly DbHelper _dbHelper;

        public Rooms(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public IEnumerable<Room> GetAll()
        {
            string query = "SELECT RoomId, RoomName, TeacherId FROM Rooms";
            DataTable dataTable = _dbHelper.ExecuteQuery(query);

            List<Room> rooms = new List<Room>();
            foreach (DataRow row in dataTable.Rows)
            {
                rooms.Add(new Room
                {
                    RoomId = Convert.ToInt32(row["RoomId"]),
                    RoomName = row["RoomName"].ToString(),
                    TeacherId = row["TeacherId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["TeacherId"]),
                });
            }

            return rooms;
        }

        public Room GetById(int id)
        {
            string query = "SELECT RoomId, RoomName, TeacherId FROM Rooms WHERE RoomId = @RoomId";
            var parameters = new Dictionary<string, object>
            {
                { "@RoomId", id }
            };

            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            DataRow row = dataTable.Rows[0];
            return new Room
            {
                RoomId = Convert.ToInt32(row["RoomId"]),
                RoomName = row["RoomName"].ToString(),
                TeacherId = row["TeacherId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["TeacherId"]),
            };
        }

        public bool Insert(Room room)
        {
            string hashedPassword = PasswordHelper.HashPassword(room.Password);

            string query = "INSERT INTO Rooms (RoomName, TeacherId, Password) VALUES (@RoomName, @TeacherId, @Password)";
            var parameters = new Dictionary<string, object>
            {
                { "@RoomName", room.RoomName },
                { "@TeacherId", room.TeacherId.HasValue ? (object)room.TeacherId.Value : DBNull.Value },
                { "@Password", hashedPassword }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Update(Room room)
        {
            string hashedPassword = PasswordHelper.HashPassword(room.Password);

            string query = "UPDATE Rooms SET RoomName = @RoomName, TeacherId = @TeacherId WHERE RoomId = @RoomId";
            var parameters = new Dictionary<string, object>
            {
                { "@RoomId", room.RoomId },
                { "@RoomName", room.RoomName },
                { "@TeacherId", room.TeacherId.HasValue ? (object)room.TeacherId.Value : DBNull.Value },
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Delete(int id)
        {
            string query = "DELETE FROM Rooms WHERE RoomId = @RoomId";
            var parameters = new Dictionary<string, object>
            {
                { "@RoomId", id }
            };

            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool Authenticate(int roomId, string password)
        {
            string query = "SELECT Password FROM Rooms WHERE RoomId = @RoomId";
            var parameters = new Dictionary<string, object>
            {
                { "@RoomId", roomId }
            };

            DataTable dataTable = _dbHelper.ExecuteQueryWithParams(query, parameters);

            if (dataTable.Rows.Count == 0)
                return false;

            string storedHash = dataTable.Rows[0]["Password"].ToString();
            return PasswordHelper.VerifyPassword(password, storedHash);
        }
    }
}
