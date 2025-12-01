using Microsoft.AspNetCore.Mvc;
using MeuProjetoMVC.Autenticacao;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize(RoleAnyOf = "Admin,User")] 
    public class ZetaJogosController : Controller
    {
        private readonly string _connectionString;

        public ZetaJogosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // LISTAGEM
        public IActionResult Index()
        {
            List<ZetaJogos> lista = new();

            using var conex = new MySqlConnection(_connectionString);
            conex.Open();
            string sql = @"SELECT z.codZetaJ, z.nomeJogo, z.codUsuario, z.classificacaoEtaria, 
                                  z.codCat, c.nomeCategoria 
                           FROM ZetaJogos z
                           LEFT JOIN Categorias c ON z.codCat = c.codCat";

            using var cmd = new MySqlCommand(sql, conex);
            using var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ZetaJogos
                {
                    codZetaJ = dr.GetInt32("codZetaJ"),
                    nomeJogo = dr.GetString("nomeJogo"),
                    codUsuario = dr.GetInt32("codUsuario"),
                    classificacaoEtaria = dr["classificacaoEtaria"] as string,
                    categoria = dr.GetInt32("codCat"),
                    nomeCategoria = dr["nomeCategoria"] as string
                });
            }

            return View(lista);
        }
        [HttpGet]

        // GET: Criar
        public IActionResult Criar()
        {
            CarregarCategorias();
            return View();
        }

        // POST: Criar
        [HttpPost]
        public IActionResult Criar(ZetaJogos jogo, IFormFile? imagemCapa,IFormFile arquivoJogo)
        {
            if (!ModelState.IsValid)
            {
                CarregarCategorias();
                return View(jogo);
            }

            var usuarioSessao = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (usuarioSessao == null)
            {
                ModelState.AddModelError("", "Usuário não autenticado.");
                CarregarCategorias();
                return View(jogo);
            }

            jogo.codUsuario = usuarioSessao.Value;

            byte[]? capaBytes = null;
            if (imagemCapa != null)
            {
                using var ms = new MemoryStream();
                imagemCapa.CopyTo(ms);
                capaBytes = ms.ToArray();
            }
             // -------------------------
    // 2. Upload do JOGO HTML
    // -------------------------
    if (arquivoJogo == null || arquivoJogo.Length == 0)
    {
        ModelState.AddModelError("", "Você deve enviar o arquivo do jogo (HTML).");
        CarregarCategorias();
        return View(jogo);
    }

    // Valida extensão
    var extensao = Path.GetExtension(arquivoJogo.FileName).ToLower();
    if (extensao != ".html" && extensao != ".htm")
    {
        ModelState.AddModelError("", "O jogo deve ser um arquivo HTML.");
        CarregarCategorias();
        return View(jogo);
    }

    // Cria pasta com nome do jogo
    string nomePasta = jogo.nomeJogo.Replace(" ", "_").ToLower();
    string pastaDestino = Path.Combine("wwwroot", "Jogos", nomePasta);

    if (!Directory.Exists(pastaDestino))
        Directory.CreateDirectory(pastaDestino);

    // Salva o arquivo
    string caminhoArquivo = Path.Combine(pastaDestino, arquivoJogo.FileName);

    using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
    {
        arquivoJogo.CopyTo(stream);
    }

   // Caminho relativo salvo no banco
    string caminhoRelativo = $"Jogos/{nomePasta}/{arquivoJogo.FileName}";
    jogo.caminhoJogo = caminhoRelativo;
    Console.WriteLine(">>> Caminho Jogo Gerado: " + jogo.caminhoJogo);
            using var conex = new MySqlConnection(_connectionString);
            conex.Open();
            string sql = @"INSERT INTO ZetaJogos (nomeJogo, codUsuario, imagemCapa, classificacaoEtaria, codCat, caminhoJogo)
                           VALUES (@nome, @user, @img, @class, @cat, @caminho)";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@nome", jogo.nomeJogo);
            cmd.Parameters.AddWithValue("@user", jogo.codUsuario);
            cmd.Parameters.AddWithValue("@img", capaBytes);
            cmd.Parameters.AddWithValue("@class", jogo.classificacaoEtaria);
            cmd.Parameters.AddWithValue("@cat", jogo.categoria);
            cmd.Parameters.AddWithValue("@caminho", jogo.caminhoJogo);


            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
         [HttpGet]
        // GET: Editar
        public IActionResult Editar(int id)
        {
            ZetaJogos? jogo = BuscarPorId(id);
            if (jogo == null) return NotFound();

            CarregarCategorias();
            return View(jogo);
        }

        // POST: Editar
        [HttpPost]
        public IActionResult Editar(ZetaJogos jogo, IFormFile? novaImagem)
        {
            if (!ModelState.IsValid)
            {
                CarregarCategorias();
                return View(jogo);
            }

            byte[]? imagemBytes = null;
            if (novaImagem != null)
            {
                using var ms = new MemoryStream();
                novaImagem.CopyTo(ms);
                imagemBytes = ms.ToArray();
            }

            using var conex = new MySqlConnection(_connectionString);
            conex.Open();

            string sql = (imagemBytes == null) ?
                @"UPDATE ZetaJogos 
                  SET nomeJogo=@nome, classificacaoEtaria=@class, codCat=@cat 
                  WHERE codZetaJ=@id" :
                @"UPDATE ZetaJogos 
                  SET nomeJogo=@nome, classificacaoEtaria=@class, codCat=@cat, imagemCapa=@img 
                  WHERE codZetaJ=@id";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@nome", jogo.nomeJogo);
            cmd.Parameters.AddWithValue("@class", jogo.classificacaoEtaria);
            cmd.Parameters.AddWithValue("@cat", jogo.categoria);
            cmd.Parameters.AddWithValue("@id", jogo.codZetaJ);
            cmd.Parameters.AddWithValue("@caminho", jogo.caminhoJogo);



            if (imagemBytes != null)
                cmd.Parameters.AddWithValue("@img", imagemBytes);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
         [HttpGet]
        // GET: Excluir
        public IActionResult Excluir(int id)
        {
            ZetaJogos? jogo = BuscarPorId(id);
            if (jogo == null) return NotFound();
            return View(jogo);
        }

        // POST: Excluir Confirmado
        [HttpPost, ActionName("Excluir")]
        public IActionResult ExcluirConfirmado(int id)
        {
            using var conex = new MySqlConnection(_connectionString);
            conex.Open();
            string sql = "DELETE FROM ZetaJogos WHERE codZetaJ=@id";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // Busca
        private ZetaJogos? BuscarPorId(int id)
        {
            using var conex = new MySqlConnection(_connectionString);
            conex.Open();
            string sql = @"SELECT z.codZetaJ, z.nomeJogo, z.codUsuario, z.classificacaoEtaria, 
                                  z.codCat, c.nomeCategoria 
                           FROM ZetaJogos z
                           LEFT JOIN Categorias c ON z.codCat = c.nomeCategoria
                           WHERE z.codZetaJ=@id";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@id", id);
            using var dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                return new ZetaJogos
                {
                    codZetaJ = dr.GetInt32("codZetaJ"),
                    nomeJogo = dr.GetString("nomeJogo"),
                    codUsuario = dr.GetInt32("codUsuario"),
                    classificacaoEtaria = dr["classificacaoEtaria"] as string,
                    categoria = dr.GetInt32("codCat"),
                    nomeCategoria = dr["nomeCategoria"] as string
                };
            }

            return null;
        }

        private void CarregarCategorias()
        {
            List<SelectListItem> categorias = new();

            using var conex = new MySqlConnection(_connectionString);
            conex.Open();
            string sql = "SELECT codCat, nomeCategoria FROM Categorias";
            using var cmd = new MySqlCommand(sql, conex);
            using var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                categorias.Add(new SelectListItem
                {
                    Value = dr.GetInt32("codCat").ToString(),
                    Text = dr.GetString("nomeCategoria")
                });
            }

            ViewBag.Categorias = categorias;
        }
        
    }
}
