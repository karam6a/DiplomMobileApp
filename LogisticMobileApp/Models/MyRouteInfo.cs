using System.Text.Json.Serialization;

namespace LogisticMobileApp.Models
{
    public class MyRouteInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("geometry_json")]
        public string GeometryJson { get; set; } = string.Empty;

        [JsonPropertyName("license_plate")]
        public string LicensePlate { get; set; } = string.Empty;
    }
}

