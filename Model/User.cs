using System.Text.Json;
using System.Text.Json.Serialization;

namespace SOLARY.Model
{
    public class User
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("status_compte")]
        public string StatusCompte { get; set; } = string.Empty;

        [JsonPropertyName("compte_verifie")]
        public bool CompteVerifie { get; set; }

        [JsonPropertyName("date_creation")]
        public DateTime DateCreation { get; set; }

        [JsonPropertyName("dernier_login")]
        public DateTime? DernierLogin { get; set; }

        [JsonPropertyName("otp_code")]
        public string? OtpCode { get; set; }

        [JsonPropertyName("otp_created_at")]
        public DateTime? OtpCreatedAt { get; set; }

        [JsonPropertyName("code_casiers")]
        [JsonConverter(typeof(StringOrNumberConverter))]
        public string? CodeCasiers { get; set; }

        // Propriétés calculées
        [JsonIgnore]
        public bool IsActive => StatusCompte.ToLower() == "actif";

        [JsonIgnore]
        public bool IsAdmin => Role.ToLower() == "admin";

        [JsonIgnore]
        public bool HasValidOtp => OtpCode != null && OtpCreatedAt.HasValue &&
                                   OtpCreatedAt.Value.AddMinutes(10) > DateTime.Now;

        [JsonIgnore]
        public bool HasValidLockerCode => !string.IsNullOrEmpty(CodeCasiers) &&
                                         CodeCasiers.Length >= 4 &&
                                         CodeCasiers.Length <= 10;
    }

    public class StringOrNumberConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64().ToString();
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            throw new JsonException($"Cannot convert {reader.TokenType} to string");
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
