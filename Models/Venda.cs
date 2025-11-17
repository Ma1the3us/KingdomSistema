namespace MeuProjetoMVC.Models
{
    public class Venda
    {
        public int? codVenda { get; set; }

        public int? codUsuario { get; set; }

        public Decimal? valorTotalVenda { get; set; }

        public string? formaPag { get; set; }

        public string? situacao { get; set; }

        public DateOnly? dataE { get; set; }
    }
}
