using System.Text.Json.Serialization;

namespace MeuProjetoMVC.Models
{
    public class OpenRouteResponse
    {
        [JsonPropertyName("routes")]
        public Route[] Routes { get; set; }
    }

    public class Route
    {
        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }

        [JsonPropertyName("segments")]
        public Segment[] Segments { get; set; }
    }

    public class Summary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    public class Segment
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    public class Coordenadas
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
