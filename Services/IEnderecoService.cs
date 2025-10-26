
namespace MeuProjetoMVC.Services
{
    public interface IEnderecoService
    {
        Task<EnderecoResponse> ObterEnderecoPorCepAsync(string cep);
    }
}
