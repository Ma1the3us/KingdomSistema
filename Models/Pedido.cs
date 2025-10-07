namespace MeuProjetoMVC.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string EmailCliente { get; set; } = string.Empty;
        public DateTime DataPedido { get; set; }
        public decimal Total { get; set; }
        public List<CartItem> Itens { get; set; } = new();
        
    }
}
