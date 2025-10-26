using System.Text.Json;
using System;

namespace MeuProjetoMVC.Services
{
    public class EnderecoService : IEnderecoService
    {
        private readonly HttpClient _httpClient;

        public EnderecoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EnderecoResponse> ObterEnderecoPorCepAsync(string cep)
        {
            // Chama a API do ViaCEP
            var response = await _httpClient.GetAsync($"https://viacep.com.br/ws/{cep}/json/");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // Desserializa o JSON para um objeto
            var endereco = JsonSerializer.Deserialize<EnderecoResponse>(json);

            return endereco;
        }
    }

    // Classe para armazenar os dados do endereço
    public class EnderecoResponse
    {
        public string Logradouro { get; set; }
        public string Bairro { get; set; }
        public string Localidade { get; set; }
        public string Uf { get; set; }
    }
}
