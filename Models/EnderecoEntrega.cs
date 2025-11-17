namespace MeuProjetoMVC.Models
{
    public class EnderecoEntrega
    {
        public int codEndereco { get; set; }

        public string? Cep { get; set; }

        public string? Logradouro { get; set; }
        public string? Estado { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }

    }
}
