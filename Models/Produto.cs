namespace MeuProjetoMVC.Models
{
    public class Produto
{
    public int codProd { get; set; }
    public int Quantidade { get; set; }
    public byte[] Imagens { get; set; } = Array.Empty<byte>();
    public string ImagemTipo { get; set; }  = string.Empty;
    public double Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string nomeProduto { get; set; } = string.Empty;
    public int? codCat { get; set; }
    public int? codF { get; set; }
}

}
