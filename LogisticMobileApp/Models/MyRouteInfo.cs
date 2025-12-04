using System.Text.Json.Serialization;

namespace LogisticMobileApp.Models
{
    public class MyRouteInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("route_name")]
        public string RouteName { get; set; } = string.Empty;

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

        [JsonPropertyName("clients_data")]
        public List<ClientData> ClientsData { get; set; } = new();
    }
}
