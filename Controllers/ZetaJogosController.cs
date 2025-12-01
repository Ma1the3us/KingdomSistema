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

        // GET: Criar
        public IActionResult Criar()
        {
            CarregarCategorias();
            return View();
        }

     
        [HttpPost]
        public IActionResult Criar(ZetaJogos jogo, IFormFile? imagemCapa, string? nomePasta)
        {
            if (!ModelState.IsValid)
            {
                CarregarCategorias();
                return View(jogo);
            }


            // -------------------------
            // 1. Gerar caminho do jogo
            // -------------------------
            if (!string.IsNullOrEmpty(nomePasta))
            {
                // Ex: /jogos/MeuJogo/index.html
                jogo.caminhoJogo = $"jogos/{nomePasta}/index.html";
            }
            else
            {
                ModelState.AddModelError("", "Informe o nome da pasta do jogo.");
                CarregarCategorias();
                return View(jogo);
            }

            // -------------------------
            // 2. Imagem da capa
            // -------------------------
            byte[]? capaBytes = null;
            if (imagemCapa != null)
            {
                using var ms = new MemoryStream();
                imagemCapa.CopyTo(ms);
                capaBytes = ms.ToArray();
            }

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
        public IActionResult Editar(ZetaJogos jogo, IFormFile? novaImagem, string? nomePasta)
        {
            if (!ModelState.IsValid)
            {
                CarregarCategorias();
                return View(jogo);
            }

            // -------------------------
            // 1. Caminho do jogo
            // -------------------------
            if (!string.IsNullOrEmpty(nomePasta))
            {
                // Usuário quer alterar a pasta → gera novo caminho
                jogo.caminhoJogo = $"jogos/{nomePasta}/index.html";
            }
            else
            {
                // Mantém o caminho antigo vindo do model
                // (em caso de nullable, pode garantir segurança)
                if (string.IsNullOrWhiteSpace(jogo.caminhoJogo))
                {
                    ModelState.AddModelError("", "O jogo não possui um caminho válido salvo.");
                    CarregarCategorias();
                    return View(jogo);
                }
            }


            // -------------------------
            // 2. Nova imagem?
            // -------------------------
            byte[]? imagemBytes = null;
            if (novaImagem != null)
            {
                using var ms = new MemoryStream();
                novaImagem.CopyTo(ms);
                imagemBytes = ms.ToArray();
            }


            using var conex = new MySqlConnection(_connectionString);
            conex.Open();

            // SQL com ou sem imagem nova
            string sql = (imagemBytes == null)
                ? @"UPDATE ZetaJogos 
             SET nomeJogo=@nome,
                 classificacaoEtaria=@class,
                 codCat=@cat,
                 caminhoJogo=@caminho
             WHERE codZetaJ=@id"
                : @"UPDATE ZetaJogos 
             SET nomeJogo=@nome,
                 classificacaoEtaria=@class,
                 codCat=@cat,
                 caminhoJogo=@caminho,
                 imagemCapa=@img
             WHERE codZetaJ=@id";

            using var cmd = new MySqlCommand(sql, conex);
            cmd.Parameters.AddWithValue("@nome", jogo.nomeJogo);
            cmd.Parameters.AddWithValue("@class", jogo.classificacaoEtaria);
            cmd.Parameters.AddWithValue("@cat", jogo.categoria);
            cmd.Parameters.AddWithValue("@caminho", jogo.caminhoJogo);
            cmd.Parameters.AddWithValue("@id", jogo.codZetaJ);

            if (imagemBytes != null)
                cmd.Parameters.AddWithValue("@img", imagemBytes);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // GET: Excluir
        public IActionResult Excluir(int id)
        {
            ZetaJogos? jogo = BuscarPorId(id);
            if (jogo == null) return NotFound();
            return View(jogo);
        }

        // POST: Excluir Confirmado
        [HttpPost, ActionName("Excluir")]
        public IActionResult Excluir(int? id)
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
                                  z.codCat, c.nomeCategoria,z.caminhoJogo 
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
                    nomeCategoria = dr["nomeCategoria"] as string,
                    caminhoJogo = dr["caminhoJogo"] as string
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
