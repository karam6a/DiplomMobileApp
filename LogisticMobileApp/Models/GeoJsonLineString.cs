using System.Text.Json.Serialization;

namespace LogisticMobileApp.Models
{
    public class GeoJsonLineString
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("coordinates")]
        public List<List<double>> Coordinates { get; set; } = new();
    }
}



