using System.ComponentModel.DataAnnotations;

namespace MeuProjetoMVC.Models
{
    public class Usuario
    {
        public int CodUsuario { get; set; }

        [Required]
        [Display(Name = "Tipo de Usuário")]
        public string? Role { get; set; } 

        [Required, StringLength(100)]
        public string? Nome { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100)]
        [Display(Name = "Senha")]
        public string? Senha { get; set; } // 🔹 removido [Required]

        [Compare("Senha", ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar Nova Senha")]
        public string? ConfirmarSenha { get; set; }

        [Display(Name = "Ativo")]
        public string? Ativo { get; set; } = "1";

        public byte[] Imagens { get; set; } = Array.Empty<byte>();

        public string? Telefone { get; set; }

    }
}
