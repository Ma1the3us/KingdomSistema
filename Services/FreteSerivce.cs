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

            // Faz uma autorização para o processo seguinte, pois a api precisa de uma validação de autenticação para poder funcionar
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            // Ele vai mandar a autentificação do content, para pegar a rota por carro.
            var response = await _httpClient.PostAsync("https://api.openrouteservice.org/v2/directions/driving-car", content);

            // Pega a resposta
            response.EnsureSuccessStatusCode();

            // Le a resposta trazida
            var responseString = await response.Content.ReadAsStringAsync();
           
            // Pega a resposta e transforma em uma rota
            // No formato da Model
            var rotaInfo = JsonSerializer.Deserialize<OpenRouteResponse>(responseString);

            // Atribui a informação da distância a rota.
            double distanciaMetros = rotaInfo.Features[0].Properties.Segments[0].Distance;
            // Faz a distância vezes a porcentagem do frete
            double frete = distanciaMetros * 0.01; // exemplo de cálculo de frete

            // Retorna o valor do frete.
            return frete;
        }

    private async Task<Coordenadas> ObterCoordenadasPorCep(string cep)
    {
        // Remove traços do CEP (01001-000 → 01001000)
        cep = cep.Replace("-", "").Trim();

        // Inseri o link, com o valor do cep integrado, dando o valor a variavel.
        var url = $"https://nominatim.openstreetmap.org/search?format=json&country=Brasil&postalcode={cep}";

        //Faz o pedido e adiciona o email o email cadastrado para quem estiver usando no momento
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "MeuProjetoMVC/1.0 (irineu32323@gmail.com)");
        
            //Ele envia o pedido
        var response = await _httpClient.SendAsync(request);
        // Pega a resposta
        response.EnsureSuccessStatusCode();

        // Transforma em Json e lê o que foi trazido
        var json = await response.Content.ReadAsStringAsync();

        // Transforma no formato daquela view com aqueles parâmetros.
        var resultados = JsonSerializer.Deserialize<List<NominatimResponse>>(json);

        //Validação
        if (resultados == null || resultados.Count == 0)
        {
            throw new Exception($"Não foi possível obter coordenadas para o CEP {cep}");
        }
        // Transforma em variavel... para receber o valor do primeiro item encontrado
        var primeiro = resultados[0];

        // Retorna o valor da latitude e longitude daquele cep.
        return new Coordenadas
        {
            Latitude = double.Parse(primeiro.lat, System.Globalization.CultureInfo.InvariantCulture),
            Longitude = double.Parse(primeiro.lon, System.Globalization.CultureInfo.InvariantCulture)
        };
        }

    }
}
