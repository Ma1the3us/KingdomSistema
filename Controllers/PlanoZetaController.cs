using Microsoft.AspNetCore.Mvc;
using MeuProjetoMVC.Autenticacao;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;

namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize] // permite qualquer usuário logado
    public class PlanoZetaController : Controller
    {
        private readonly string _connectionString;

        public PlanoZetaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Página inicial do plano Zeta
        public IActionResult Index()
        {
            int? codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            // Se o usuário já tem plano → ir para os jogos
            if (UsuarioTemPlano(codUsuario.Value))
                return RedirectToAction("ZetaJogos");

            return View();
        }

        // Tela de contratação
        public IActionResult Contratar()
        {
            int? codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            if (UsuarioTemPlano(codUsuario.Value))
                return RedirectToAction("ZetaJogos");

            return View();
        }

        // Confirmação do pagamento
        public IActionResult ConfirmarPagamento()
        {
            int? codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string sql = "UPDATE Usuario SET PlanoZeta = 'S' WHERE codUsuario = @id";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", codUsuario.Value);
                cmd.ExecuteNonQuery();

                HttpContext.Session.SetString(SessionKeys.UserPlanoZeta, "S");

                return RedirectToAction("ZetaJogos");
            }
            catch
            {
                return View("Erro");
            }
        }

        // Jogos para usuário com plano ativo
        public IActionResult ZetaJogos()
        {
            int? codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            if (!UsuarioTemPlano(codUsuario.Value))
                return RedirectToAction("Contratar");

            List<ZetaJogos> jogos = new();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string sql = "SELECT * FROM ZetaJogos ORDER BY codZetaJ DESC";

            using var cmd = new MySqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                jogos.Add(new ZetaJogos
                {
                    codZetaJ = reader.GetInt32("codZetaJ"),
                    nomeJogo = reader.GetString("nomeJogo"),
                    categoria = reader.GetInt32("codCat"),
                    classificacaoEtaria = reader.GetString("classificacaoEtaria"),
                    Capa = reader["imagemCapa"] as byte[] ?? new byte[0]
                });
            }

            return View(jogos);
        }

        private bool UsuarioTemPlano(int codUsuario)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string sql = "SELECT PlanoZeta FROM Usuario WHERE codUsuario = @id";

            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", codUsuario);

            string? plano = cmd.ExecuteScalar()?.ToString();

            return plano == "S";
        }



        public IActionResult Jogar(int id)
        {
            var jogo = BuscarPorId(id);
            if (jogo == null)
                return NotFound();

            // Se já existe um caminho correto, manda para a view que roda o jogo
            return View("Jogar", jogo);
        }

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

    }
}
