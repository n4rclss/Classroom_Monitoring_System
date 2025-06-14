﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Student.MessageModel
{
    public class Screen_data
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "screen_data";

        [JsonPropertyName("image_data")]
        public string image_data { get; set; }

        [JsonPropertyName("sender_client_id")]
        public string sender_client_id { get; set; }




    }

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

        [JsonPropertyName("sender_client_id")]
        public string sender_client_id { get; set; }

        [JsonPropertyName("app_data")]
        public List<ProcessInfo> app_data { get; set; } = new List<ProcessInfo>();

        public class ProcessInfo
        {
            [JsonPropertyName("process_name")]
            public string ProcessName { get; set; }

            [JsonPropertyName("main_window_title")]
            public string MainWindowTitle { get; set; }
        }

       
    }
}
