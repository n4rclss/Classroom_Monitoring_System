using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Teacher.MessageModel
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
        public string Role { get; set; } = "teacher"; // Default role for teacher
    }

    public class JoinClassMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "join_class";

        [JsonPropertyName("class_id")]
        public string ClassId { get; set; }
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