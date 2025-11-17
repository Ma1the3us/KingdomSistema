using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using MeuProjetoMVC.Models;
using System;
using System.Collections.Generic;

namespace MeuProjetoMVC.Services
{
    public class FreteService : IFreteServices
    {
        private readonly HttpClient _httpClient;

        // 🔑 SUA API KEY DO GEOAPIFY
        private readonly string _geoapifyKey = "24659789fc354c88a12272282589a9c5";

        // 🔑 SUA API KEY DO OPENROUTESERVICE
        private readonly string _orsApiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjBmNTI2ZTU2ZjY2NGQwYTdkOGIzODMyNDJmYWE1N2E3OGIyYWE1NDRiMjYyZTg1MjRmNDRiMGIyIiwiaCI6Im11cm11cjY0In0=";

        public FreteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // =========================================================
        //  CALCULAR FRETE
        // =========================================================
        public async Task<double> CalcularFreteAsync(string cepOrigem, string cepDestino)
        {
            // 1️⃣ Coordenadas via Geoapify
            var origemCoords = await ObterCoordenadasPorCep(cepOrigem);
            var destinoCoords = await ObterCoordenadasPorCep(cepDestino);

            // 2️⃣ Monta payload da rota
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

            // 3️⃣ Autorização para ORS
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _orsApiKey);

            // 4️⃣ Chamada à API de rota
            var response = await _httpClient.PostAsync(
                "https://api.openrouteservice.org/v2/directions/driving-car",
                content
            );

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // 🔍 Debug
            System.Diagnostics.Debug.WriteLine("\n========= ORS RAW RESPONSE =========");
            System.Diagnostics.Debug.WriteLine(responseString);
            System.Diagnostics.Debug.WriteLine("========= END RESPONSE =========\n");

            var rotaInfo = JsonSerializer.Deserialize<OpenRouteResponse>(responseString);

            if (rotaInfo?.Routes == null || rotaInfo.Routes.Length == 0)
                throw new Exception("A API de rotas não retornou resultado válido.");

            double distanciaMetros = rotaInfo.Routes[0].Segments[0].Distance;

            // 5️⃣ Cálculo simples do frete (ajuste conforme sua regra)
            double frete = distanciaMetros * 0.01;

            return frete;
        }

        // =========================================================
        //  GEOAPIFY — COORDENADAS A PARTIR DO CEP
        // =========================================================
        private async Task<Coordenadas> ObterCoordenadasPorCep(string cep)
        {
            cep = cep.Replace("-", "").Trim();

            // 1️⃣ Usa ViaCep para montar o endereço completo
            var viaCepResp = await _httpClient.GetAsync($"https://viacep.com.br/ws/{cep}/json/");
            viaCepResp.EnsureSuccessStatusCode();

            var viaCepJson = await viaCepResp.Content.ReadAsStringAsync();
            var viaCep = JsonSerializer.Deserialize<EnderecoViaCep>(viaCepJson);

            if (viaCep == null || viaCep.erro == true)
                throw new Exception($"CEP inválido: {cep}");

            // Endereço completo para geocodificação
            string endereco = $"{viaCep.logradouro}, {viaCep.localidade}, {viaCep.uf}, Brasil";

            // 2️⃣ Chamada Geoapify
            string url =
                $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(endereco)}&format=json&apiKey={_geoapifyKey}";

            var respGeo = await _httpClient.GetAsync(url);
            respGeo.EnsureSuccessStatusCode();

            var geoJson = await respGeo.Content.ReadAsStringAsync();

            // 🔍 Debug
            System.Diagnostics.Debug.WriteLine("\n========= GEOAPIFY RAW REPONSE =========");
            System.Diagnostics.Debug.WriteLine(geoJson);
            System.Diagnostics.Debug.WriteLine("========= END RESPONSE =========\n");

            var geoData = JsonSerializer.Deserialize<GeoapifyResponse>(geoJson);

            if (geoData == null || geoData.results == null || geoData.results.Count == 0)
                throw new Exception($"Geoapify não encontrou coordenadas para: {endereco}");

            var r = geoData.results[0];

            return new Coordenadas
            {
                Latitude = r.lat,
                Longitude = r.lon
            };
        }
    }

    // =========================================================
    //     MODELOS
    // =========================================================
    public class EnderecoViaCep
    {
        public string cep { get; set; }
        public string logradouro { get; set; }
        public string localidade { get; set; }
        public string uf { get; set; }
        public bool? erro { get; set; }
    }

    public class GeoapifyResponse
    {
        public List<GeoapifyResult> results { get; set; }
    }

    public class GeoapifyResult
    {
        public double lat { get; set; }
        public double lon { get; set; }
    }
}
