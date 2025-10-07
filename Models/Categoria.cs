using System.ComponentModel.DataAnnotations;

namespace MeuProjetoMVC.Models
{
    public class Categoria
    {
        [Key]
        public int CodCat { get; set; }

        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome da categoria deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome da Categoria")]
        public string NomeCategoria { get; set; } = string.Empty;
    }
}
