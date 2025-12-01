namespace MeuProjetoMVC.Models
{
    public class ZetaJogos
    {
        public int? codZetaJ { get; set; }  
         public string? nomeCategoria{ get; set; }
        public string? nomeJogo { get; set; }
        public byte[]? Jogo { get; set; }
        public string? jogoTipo { get; set; }
        public byte[]? Capa { get; set; } = Array.Empty<byte>();       
        public string? classificacaoEtaria { get; set; }
        public int? categoria { get; set; }
        public string? caminhoJogo { get; set; }

    }
}