using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Teacher.MessageModel
{
    public class Streaming_message
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "streaming";

        [JsonPropertyName("target_username")]
        public string target_username { get; set; }

     
    }

    public class Notify
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "notify";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

        [JsonPropertyName("noti_message")]
        public string noti_message { get; set; }
    }
    public class Refresh
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "refresh";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

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
        public string Role { get; set; } = "teacher"; // Default role for teacher
    }

    // Response message structure from server
    public class LoginResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }


    public class Create_room
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "create_room";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

        [JsonPropertyName("teacher")]
        public string teacher { get; set; }
    }

    public class Log_out
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "logout";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

        [JsonPropertyName("teacher")]
        public string teacher { get; set; }
    }
    // message request running apps
    public class RequestRunningApps
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "request_app";
        [JsonPropertyName("target_username")]
        public string target_username { get; set; }
    }

    public class ProcessInfo
    {
        [JsonPropertyName("process_name")]
        public string process_name { get; set; }

        [JsonPropertyName("main_window_title")]
        public string main_window_title { get; set; }
    }
}
