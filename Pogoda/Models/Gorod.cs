using System;
using Newtonsoft.Json;

namespace Pogoda.Models
{
    public class Gorod
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("country")]
        public string? Country { get; set; }

        [JsonProperty("population")]
        public int Population { get; set; }

        [JsonProperty("timezone")]
        public int Timezone { get; set; }

        [JsonProperty("sunrise")]
        public long Sunrise { get; set; }

        [JsonProperty("sunset")]
        public long Sunset { get; set; }

        public DateTime SunriseTime => DateTimeOffset.FromUnixTimeSeconds(Sunrise).LocalDateTime;
        public DateTime SunsetTime => DateTimeOffset.FromUnixTimeSeconds(Sunset).LocalDateTime;
    }

    public class GorodInfo
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
    }
}