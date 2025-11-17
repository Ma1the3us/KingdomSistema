namespace MeuProjetoMVC.Models
{
    public class ZetaJogos
    {
        public int? codZetaJ { get; set; }

        public string? nomeJogo { get; set; }

        public byte[]? Jogo { get; set; }

        public string? jogoTipo { get; set; }

        public string? codZetaV { get; set; }

        public byte[] Capa { get; set; } = Array.Empty<byte>();
        public string? classificacaoEtaria { get; set; }

        public string? categoria { get; set; }

    }
}
