namespace MeuProjetoMVC.Models
{
    public class CartItem
    {
        public int codCarrinho { get; set; }
        public int codProd { get; set; }
        public int Quantidade { get; set; }
         public decimal ValorUnitario { get; set; }

        public decimal Total => ValorUnitario * Quantidade;
    }
}
