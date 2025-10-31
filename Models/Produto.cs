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

        public double? Valor { get; set; }
    
        public int? codCat { get; set; }
        
        public int? codF { get; set; }

        public double? Desconto { get; set; }
    }

}
