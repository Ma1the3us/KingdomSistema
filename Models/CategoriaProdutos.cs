namespace MeuProjetoMVC.Models
{
    public class CategoriaProdutos
    {
        public int codCat { get; set; }

        public string? nomeCategoria { get; set; }

        public List<Produto>? Produtos { get; set; } = new List<Produto>();
    }
}
