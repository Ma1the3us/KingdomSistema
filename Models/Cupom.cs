namespace MeuProjetoMVC.Models
{
    public class Cupom
    {
        public int? codCupom { get; set; }

        public int? codUsuario { get; set; }

        public string? codigo { get; set; }

        public string? desconto { get; set; }

        public DateTime? dataCriacao { get; set; }

        public DateTime? dataValidade { get; set; }

        public bool? usado { get; set; }

        public bool? ativo { get; set; }
    }
}
