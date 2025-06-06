using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Student.MessageModel
{
    public class LoginMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "login";

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "student"; // Default role for student
    }


    public class Join_room
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "join_room";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

        [JsonPropertyName("student_name")]
        public string student_name { get; set; }

        [JsonPropertyName("mssv")]
        public string mssv { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    // Response message structure from server
    public class LoginResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
    public class RunningAppMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "return_app";

        [JsonPropertyName("room_id")]
        public string RoomId { get; set; }
        [JsonPropertyName("teacher_username")]
        public string TeacherUsername { get; set; } // teacher's username
       
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("apps")]
        public List<ProcessInfo> Apps { get; set; } = new List<ProcessInfo>();

        public class ProcessInfo
        {
            [JsonPropertyName("process_name")]
            public string ProcessName { get; set; }

            [JsonPropertyName("main_window_title")]
            public string MainWindowTitle { get; set; }
        }

        /*
         {
            type: "return_app",
            room_id: "1",
            teacher_username: "teacher1",
            username: "student1",
            apps: [
                      {
                        "process_name": "devenv",
                        "main_window_title": "Classroom_Monitoring_System - Microsoft Visual Studio (Administrator)"
                      },
                      {
                        "process_name": "TextInputHost",
                        "main_window_title": "Windows Input Experience"
                      },
                      {
                        "process_name": "explorer",
                        "main_window_title": ""
                      }
                  ]
         }
         */
    }
}
