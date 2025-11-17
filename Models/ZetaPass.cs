namespace MeuProjetoMVC.Models
{
    public class ZetaPass
    {

        public int codZeta { get; set; }

        public DateOnly? dataInicial { get; set; }

        public DateOnly? dataFinal { get; set; }

        public int? codUsuario { get; set; }

        public int? codZetaV { get; set; }

        public string? formaPag { get; set; }

        public string? situacao { get; set; }
    }
}
