using System.Text.Json.Serialization;

namespace SOLARY.Model
{
    public class Borne
    {
        [JsonPropertyName("borne_id")]
        public int BorneId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("postal_code")]
        public string PostalCode { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("power_output")]
        public double PowerOutput { get; set; }

        [JsonPropertyName("charge_percentage")]
        public int ChargePercentage { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("is_in_maintenance")]
        public bool IsInMaintenance { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("last_used_at")]
        public DateTime? LastUsedAt { get; set; }

        [JsonPropertyName("distance")]
        public double? Distance { get; set; }
    }
}
