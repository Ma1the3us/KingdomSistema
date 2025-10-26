namespace MeuProjetoMVC.Models
{
    public class Comprovante
    {
        public int? codComp { get; set; }

        public int? codVenda { get; set; }

        public DateTime? dataHora { get; set; }

        public double? valorTotal { get; set; }

        public int? codUsuario { get; set; }
    }
}
