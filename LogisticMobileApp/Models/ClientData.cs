using System.Text.Json.Serialization;

namespace LogisticMobileApp.Models
{
    public class ClientData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("coordinates")]
        public string Coordinates { get; set; } = string.Empty;

        [JsonPropertyName("container_count")]
        public int ContainerCount { get; set; }
    }
}

