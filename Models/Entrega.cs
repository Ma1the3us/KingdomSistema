namespace MeuProjetoMVC.Models
{
    public class Entrega
    {
        public int codEntrega { get; set; }

        public int? codVenda { get; set; }
                    
        public int? codUsuario { get; set; }

        public double? valorTotal { get; set; }

        public int? codEnd { get; set; }

        public string? Numero { get; set; }

        public string? Complemento { get; set; }

        public string? tipoEndereco { get; set; }

        public string? Andar { get; set; }

        public string? NomePredio { get; set; }

        public string? Situacao { get; set; }

        public DateOnly? dataInicial { get; set; }

        public DateOnly? dataFinal { get; set; }

        public string? nomeDestinatario { get; set; }

        public string? emailDestinatario { get; set; }

        public string? retirada { get; set; }

    }
}
