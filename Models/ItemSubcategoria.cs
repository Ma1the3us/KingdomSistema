namespace MeuProjetoMVC.Models
{
    public class ItemSubcategoria
    {
        public int? codSub { get; set; }

        public int? codProd { get; set; }


        List<Sub_Categoria> Sub_Categoria { get; set; } 
    }
}
