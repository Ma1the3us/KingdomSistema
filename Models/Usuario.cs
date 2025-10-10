using System.ComponentModel.DataAnnotations;

namespace MeuProjetoMVC.Models
{
    public class Usuario
    {
        public int CodUsuario { get; set; }

        [Required]
        [Display(Name = "Tipo de Usuário")]
        public string Role { get; set; } = "Cliente"; // Enum: Funcionario, Admin, Cliente

        [Required, StringLength(100)]
        public string? Nome { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Senha")]
        public string? Senha { get; set; }

        [Compare("Senha", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmarSenha { get; set; }

        [Display(Name = "Ativo")]
        public string Ativo { get; set; } = "S"; // S ou N
    }
}
