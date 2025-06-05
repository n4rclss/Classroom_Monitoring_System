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

    // Response message structure from server
    public class LoginResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
