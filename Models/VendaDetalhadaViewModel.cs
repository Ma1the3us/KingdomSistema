namespace MeuProjetoMVC.Models
{
    public class VendaDetalhadaViewModel
    {
        public int CodVenda { get; set; } // Código da venda
        public double ValorTotalVenda { get; set; } // Valor total da venda
        public string FormaPagamento { get; set; } // Forma de pagamento (Débito, Crédito, Pix)
        public string situacao { get; set; } // Situação da venda (Em andamento, Finalizada, Cancelada)
        public DateTime DataVenda { get; set; } // Data da venda
        public List<ProdutoVendaViewModel> Produtos { get; set; } // Lista de produtos vendidos
    }

    // Model para detalhes dos produtos vendidos
    public class ProdutoVendaViewModel
    {
        public string NomeProduto { get; set; }
        public int Quantidade { get; set; }
        public double Valor { get; set; }
    }
}
