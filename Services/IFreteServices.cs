namespace MeuProjetoMVC.Services
{
    public interface IFreteServices
    {
        Task<double> CalcularFreteAsync(string cepOrigem, string cepDestino);
    }
}
