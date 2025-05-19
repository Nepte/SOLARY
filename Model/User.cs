using System.Text.Json.Serialization;

namespace SOLARY.Model
{
    public class User
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("status_compte")]
        public string StatusCompte { get; set; } = string.Empty;

        [JsonPropertyName("compte_verifie")]
        public bool CompteVerifie { get; set; }

        [JsonPropertyName("date_creation")]
        public DateTime DateCreation { get; set; }
    }
}
