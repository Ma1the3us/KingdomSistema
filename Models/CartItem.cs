namespace MeuProjetoMVC.Models
{
    public class CartItem
    {
        public int ProdutoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Quantidade { get; set; }

        public decimal Total => Preco * Quantidade;
    }
}
