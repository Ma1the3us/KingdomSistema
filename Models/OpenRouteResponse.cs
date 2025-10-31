using System.Text.Json.Serialization;

namespace MeuProjetoMVC.Models
{
    public class OpenRouteResponse
    {
        [JsonPropertyName("features")]
        public Feature[] Features { get; set; }
    }

    public class Feature
    {
        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }
    }

    public class Properties
    {
        [JsonPropertyName("segments")]
        public Segment[] Segments { get; set; }
    }

    public class Segment
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; } // opcional
    }

    public class Coordenadas
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
