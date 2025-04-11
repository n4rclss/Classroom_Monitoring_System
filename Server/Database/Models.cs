using System;

namespace App.Server.Database.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public int? RoomId { get; set; }
    }

    public class Teacher
    {
        public int TeacherId { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
    }

    public class Room
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int? TeacherId { get; set; }
        public string Password { get; set; }
    }
}