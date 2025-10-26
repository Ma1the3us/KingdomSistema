using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using MeuProjetoMVC.Models;

namespace MeuProjetoMVC.Services
{
    public class FreteSerivce : IFreteServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjM1NmI0YWYwYjdiZjQzOTM4NTcyNWVmMDI3ZWI3Mjg5IiwiaCI6Im11cm11cjY0In0=";

        public FreteSerivce(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<double> CalcularFreteAsync(string cepOrigem, string cepDestino)
        {
            var origemCoords = await ObterCoordenadasPorCep(cepOrigem);
            var destinoCoords = await ObterCoordenadasPorCep(cepDestino);

            var rotaPayload = new
            {
                coordinates = new[]
                {
                new[] { origemCoords.Longitude, origemCoords.Latitude },
                new[] { destinoCoords.Longitude, destinoCoords.Latitude }
            }
            };

            var jsonPayload = JsonSerializer.Serialize(rotaPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.openrouteservice.org/v2/directions/driving-car", content);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var rotaInfo = JsonSerializer.Deserialize<OpenRouteResponse>(responseString);

            double distanciaMetros = rotaInfo.Features[0].Properties.Segments[0].Distance;
            double frete = distanciaMetros * 0.01; // exemplo de cálculo de frete

            return frete;
        }

        private async Task<Coordenadas> ObterCoordenadasPorCep(string cep)
        {
            // Exemplo: para teste, retorna coordenadas fixas
            // Aqui você pode integrar com ViaCEP + algum serviço geocoding (ex: Nominatim, Google Maps)

            await Task.Delay(10);
            return new Coordenadas { Latitude = -23.55052, Longitude = -46.633308 };
        }
    }
}
