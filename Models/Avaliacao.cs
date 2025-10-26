namespace MeuProjetoMVC.Models
{
    public class Avaliacao
    {
        public int codAvaliacao { get; set; }

        public int? codProd { get; set; }

        public int? codUsuario { get; set; }

        public int? nota { get; set; }  
    
        public string? comentario { get; set; }

        public DateTime? dataAvaliacao { get; set; }
    }
}
