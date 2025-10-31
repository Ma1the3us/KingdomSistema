using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using MeuProjetoMVC.Models;

namespace MeuProjetoMVC.Services
{
    public class FreteSerivce : IFreteServices
    {
        // HttpCliente, providencia pesquisas que serão realizadas via http
        private readonly HttpClient _httpClient;
        // Chave do OpenRouteResponse Máximo de 1000 gratuitamente.
        private readonly string _apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjM1NmI0YWYwYjdiZjQzOTM4NTcyNWVmMDI3ZWI3Mjg5IiwiaCI6Im11cm11cjY0In0=";

        public FreteSerivce(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<double> CalcularFreteAsync(string cepOrigem, string cepDestino)
        {
            var origemCoords = await ObterCoordenadasPorCep(cepOrigem);
            var destinoCoords = await ObterCoordenadasPorCep(cepDestino);

            // Ele cria uma variavel, onde vai armazenar um array que vai guardar as corneadas de origem e do destino. Latitude e Longitude.
            var rotaPayload = new
            {
                coordinates = new[]
                {
                new[] { origemCoords.Longitude, origemCoords.Latitude },
                new[] { destinoCoords.Longitude, destinoCoords.Latitude }
            }
            };

            // Transforma os valores no formato de Json
            var jsonPayload = JsonSerializer.Serialize(rotaPayload);
            // Vai criar uma nova linha de string, codificando para utf8, transformando em Json a variavel.
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
    // Remove traços do CEP (01001-000 → 01001000)
    cep = cep.Replace("-", "").Trim();

    var url = $"https://nominatim.openstreetmap.org/search?format=json&country=Brasil&postalcode={cep}";

    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Add("User-Agent", "MeuProjetoMVC/1.0 (irineu32323@gmail.com)");

    var response = await _httpClient.SendAsync(request);

    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync();
    var resultados = JsonSerializer.Deserialize<List<NominatimResponse>>(json);

    if (resultados == null || resultados.Count == 0)
    {
        throw new Exception($"Não foi possível obter coordenadas para o CEP {cep}");
    }

    var primeiro = resultados[0];
    return new Coordenadas
    {
        Latitude = double.Parse(primeiro.lat, System.Globalization.CultureInfo.InvariantCulture),
        Longitude = double.Parse(primeiro.lon, System.Globalization.CultureInfo.InvariantCulture)
    };
}

    }
}
