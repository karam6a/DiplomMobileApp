using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LogisticMobileApp.Models
{
    public class DriverInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string Phone_number { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool Is_active { get; set; }
    }
}
