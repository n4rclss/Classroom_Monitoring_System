using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Teacher.MessageModel
{
    public class LoginMessage
    {
        [JsonPropertyName("type")]
        // "login" might be the message server send back, hieu guess so, a little ambigious about Ly code there.
        public string Type { get; set; } = "login";

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "teacher"; // Default role for teacher
    }

    public class Send_message_to_all
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "send_message_to_all";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

        [JsonPropertyName("message_to_all")]
        public string message_to_all { get; set; }
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

    public class Refresh
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "refresh";

        [JsonPropertyName("room_id")]
        public string room_id { get; set; }

    }


    public class ChatMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "chat_message";

        [JsonPropertyName("sender_id")]
        public string SenderId { get; set; }

        [JsonPropertyName("receiver_id")]
        public string ReceiverId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        public bool IsValidTimestamp()
        {
            return Regex.IsMatch(Timestamp, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$");
        }
    }
}