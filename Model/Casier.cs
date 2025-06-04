using System;
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace SOLARY.Model
{
    public class Casier
    {
        [JsonPropertyName("casier_id")]
        public int CasierId { get; set; }

        [JsonPropertyName("borne_id")]
        public int BorneId { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "libre"; // libre, reserve, occupe

        [JsonPropertyName("date_reservation")]
        public DateTime? DateReservation { get; set; }

        [JsonPropertyName("date_occupation")]
        public DateTime? DateOccupation { get; set; }

        // Propriétés calculées
        [JsonIgnore]
        public bool IsAvailable => Status.ToLower() == "libre";

        [JsonIgnore]
        public bool IsReserved => Status.ToLower() == "reserve";

        [JsonIgnore]
        public bool IsOccupied => Status.ToLower() == "occupe";

        [JsonIgnore]
        public string StatusText => Status.ToLower() switch
        {
            "libre" => "Libre",
            "reserve" => "Réservé",
            "occupe" => "Occupé",
            _ => "Inconnu"
        };

        [JsonIgnore]
        public Color StatusColor => Status.ToLower() switch
        {
            "libre" => Color.FromArgb("#4CAF50"),
            "reserve" => Color.FromArgb("#FFD602"),
            "occupe" => Color.FromArgb("#F44336"),
            _ => Color.FromArgb("#BBBBBB")
        };
    }
}
