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
            string sql = @"SELECT z.codZetaJ, z.nomeJogo, z.classificacaoEtaria, 
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
        public IActionResult Criar(ZetaJogos jogo, IFormFile? imagemCapa, List<IFormFile> pastaJogo)
        {
            if (!ModelState.IsValid)
            {
                CarregarCategorias();
                return View(jogo);
            }

            // ==========================
            // 0. VALIDAÇÃO DA PASTA
            // ==========================
            if (pastaJogo == null || pastaJogo.Count == 0)
            {
                ModelState.AddModelError("", "Você deve selecionar a pasta do jogo.");
                CarregarCategorias();
                return View(jogo);
            }

            // ==========================
            // 1. CAPA DO JOGO
            // ==========================
            byte[]? capaBytes = null;

            if (imagemCapa != null)
            {
                using var ms = new MemoryStream();
                imagemCapa.CopyTo(ms);
                capaBytes = ms.ToArray();
            }

            // ==========================
            // 2. CRIAR A PASTA DO JOGO
            // ==========================
            string nomePasta = jogo.nomeJogo.Replace(" ", "_").ToLower();
            string pastaDestino = Path.Combine("wwwroot", "Jogos", nomePasta);

            Directory.CreateDirectory(pastaDestino);

            // ==========================
            // 3. SALVAR TODOS ARQUIVOS
            // ==========================
            string? caminhoIndex = null;

            foreach (var arquivo in pastaJogo)
            {
                // Exemplo de filename vindo do navegador:
                // "assets/js/game.js"
                // "index.html"
                string caminhoRelativo = arquivo.FileName;

                // Caminho físico final
                string destinoFinal = Path.Combine(pastaDestino, caminhoRelativo);

                // Cria subpastas automaticamente
                Directory.CreateDirectory(Path.GetDirectoryName(destinoFinal)!);

                using var stream = new FileStream(destinoFinal, FileMode.Create);
                arquivo.CopyTo(stream);

                // Detecta automaticamente o index.html
                if (caminhoRelativo.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
                {
                    caminhoIndex = $"Jogos/{nomePasta}/{caminhoRelativo.Replace("\\", "/")}";
                }
            }

            // ==========================
            // 4. GARANTIR QUE INDEX EXISTE
            // ==========================
            if (caminhoIndex == null)
            {
                ModelState.AddModelError("", "A pasta não contém um arquivo index.html.");
                CarregarCategorias();
                return View(jogo);
            }

            // Salva no banco o caminho relativo
            jogo.caminhoJogo = caminhoIndex;


            // ==========================
            // 5. SALVAR NO BANCO
            // ==========================
            using var conex = new MySqlConnection(_connectionString);
            conex.Open();

            string sql = @"INSERT INTO ZetaJogos 
                   (nomeJogo, imagemCapa, classificacaoEtaria, codCat, caminhoJogo)
                   VALUES (@nome, @img, @class, @cat, @caminho)";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@nome", jogo.nomeJogo);
            cmd.Parameters.AddWithValue("@img", capaBytes);
            cmd.Parameters.AddWithValue("@class", jogo.classificacaoEtaria);
            cmd.Parameters.AddWithValue("@cat", jogo.categoria);
            cmd.Parameters.AddWithValue("@caminho", jogo.caminhoJogo);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult Editar(int id)
        {
            using var conex = new MySqlConnection(_connectionString);
            conex.Open();

            string sql = "SELECT * FROM ZetaJogos WHERE codZetaJ = @id";
            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return NotFound();
            }

            var jogo = new ZetaJogos
            {
                codZetaJ = reader.GetInt32("codZetaJ"),
                nomeJogo = reader.GetString("nomeJogo"),
                classificacaoEtaria = reader.GetString("classificacaoEtaria"),
                categoria = reader.GetInt32("codCat"),
                caminhoJogo = reader.GetString("caminhoJogo"),
                Capa = reader["imagemCapa"] as byte[]
            };

            CarregarCategorias();
            return View(jogo);
        }

        // POST: Editar
        [HttpPost]
        public IActionResult Editar(ZetaJogos jogo, IFormFile? imagemCapa, List<IFormFile>? pastaJogo)
        {
            if (!ModelState.IsValid)
            {
                CarregarCategorias();
                return View(jogo);
            }

            // ==========================
            // 1. CAPA DO JOGO (opcional)
            // ==========================
            byte[]? capaBytes = jogo.Capa; // mantém a capa antiga se não houver nova

            if (imagemCapa != null)
            {
                using var ms = new MemoryStream();
                imagemCapa.CopyTo(ms);
                capaBytes = ms.ToArray();
            }

            // ==========================
            // 2. SALVAR NOVA PASTA (opcional)
            // ==========================
            string caminhoIndex = jogo.caminhoJogo; // mantém caminho antigo

            if (pastaJogo != null && pastaJogo.Count > 0)
            {
                string nomePasta = jogo.nomeJogo.Replace(" ", "_").ToLower();
                string pastaDestino = Path.Combine("wwwroot", "Jogos", nomePasta);

                // Limpa a pasta antiga se existir
                if (Directory.Exists(pastaDestino))
                {
                    Directory.Delete(pastaDestino, true);
                }
                Directory.CreateDirectory(pastaDestino);

                foreach (var arquivo in pastaJogo)
                {
                    string caminhoRelativo = arquivo.FileName;
                    string destinoFinal = Path.Combine(pastaDestino, caminhoRelativo);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinoFinal)!);

                    using var stream = new FileStream(destinoFinal, FileMode.Create);
                    arquivo.CopyTo(stream);

                    if (caminhoRelativo.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        caminhoIndex = $"Jogos/{nomePasta}/{caminhoRelativo.Replace("\\", "/")}";
                    }
                }

                if (caminhoIndex == null)
                {
                    ModelState.AddModelError("", "A pasta não contém um arquivo index.html.");
                    CarregarCategorias();
                    return View(jogo);
                }
            }

            // ==========================
            // 3. ATUALIZAR NO BANCO
            // ==========================
            using var conex = new MySqlConnection(_connectionString);
            conex.Open();

            string sql = @"UPDATE ZetaJogos
                   SET nomeJogo = @nome,
                       imagemCapa = @img,
                       classificacaoEtaria = @class,
                       codCat = @cat,
                       caminhoJogo = @caminho
                   WHERE codZetaJ = @id";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@nome", jogo.nomeJogo);
            cmd.Parameters.AddWithValue("@img", (object?)capaBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@class", jogo.classificacaoEtaria);
            cmd.Parameters.AddWithValue("@cat", jogo.categoria);
            cmd.Parameters.AddWithValue("@caminho", caminhoIndex);
            cmd.Parameters.AddWithValue("@id", jogo.codZetaJ);

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
            string sql = @"SELECT z.codZetaJ, z.nomeJogo, z.classificacaoEtaria, 
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