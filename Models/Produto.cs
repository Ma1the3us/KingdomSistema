namespace MeuProjetoMVC.Models
{
    public class Produto
    {
        public int codProd { get; set; }

        public string nomeProduto { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public int? Quantidade { get; set; }

        public int? quantidadeTotal { get; set; }

        public byte[] Imagens { get; set; } = Array.Empty<byte>();

        public decimal? Valor { get; set; }      // ALTERADO PARA DECIMAL

        public int? codCat { get; set; }

        public int? codF { get; set; }

        public decimal? Desconto { get; set; }   // ALTERADO PARA DECIMAL
    }
}