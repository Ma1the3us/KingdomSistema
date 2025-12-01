namespace MeuProjetoMVC.Models
{
    public class cartaoCli
    {
        public string?  Numero {get;set;}
        public int? codCart { get; set; }


        public string? digitos { get; set; }

        public string? bandeira { get; set; }

        public int? codUsuario { get; set; } 
    
        public string? tipoCart { get; set; }

        public string? dataVencimento { get; set; } // formato MM/YY
    }
}
