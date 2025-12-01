using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;

namespace MeuProjetoMVC.Controllers
{
    [Route("sistema/avaliacao")]
    public class AvaliacaoController : Controller
    {
        private readonly string _connectionString;

        public AvaliacaoController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // -------------------------------
        // 1) INSERIR AVALIAÇÃO
        // -------------------------------
        [HttpPost("inserir")]
        public IActionResult Inserir([FromBody] Avaliacao dados)
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return Json(new { erro = "Usuário não autenticado." });

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("inserir_avaliacao ", conn) { CommandType = System.Data.CommandType.StoredProcedure};

            cmd.Parameters.AddWithValue("p_codProd", dados.codProd);
            cmd.Parameters.AddWithValue("p_codUsuario", codUsuario);
            cmd.Parameters.AddWithValue("p_nota", dados.nota);
            cmd.Parameters.AddWithValue("p_comentario", dados.comentario);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var msgSuccess = reader["Sucesso"]?.ToString();

                return Json(new { sucesso = msgSuccess });
            }

            return Json(new { erro = "Erro inesperado." });
        }


        // -------------------------------
        // 2) ATUALIZAR AVALIAÇÃO
        // -------------------------------
        [HttpPost("atualizar")]
        public IActionResult Atualizar([FromBody] Avaliacao dados)
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return Json(new { erro = "Usuário não autenticado." });

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(" atualizar_avaliacao", conn) { CommandType = System.Data.CommandType.StoredProcedure};

            cmd.Parameters.AddWithValue("p_codProd", dados.codProd);
            cmd.Parameters.AddWithValue("p_codUsuario", codUsuario);
            cmd.Parameters.AddWithValue("p_nota", dados.nota);
            cmd.Parameters.AddWithValue("p_comentario", dados.comentario);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var msgSuccess = reader["Sucesso"]?.ToString();

                return Json(new { sucesso = msgSuccess });
            }

            return Json(new { erro = "Erro inesperado." });
        }


        // -------------------------------
        // 3) DELETAR AVALIAÇÃO
        // -------------------------------
        [HttpPost("deletar")]
        public IActionResult Deletar([FromBody] Avaliacao dados)
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return Json(new { erro = "Usuário não autenticado." });

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("deletar_avaliacao", conn) { CommandType = System.Data.CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_codProd", dados.codProd);
            cmd.Parameters.AddWithValue("p_codUsuario", codUsuario);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var msgSuccess = reader["Sucesso"]?.ToString();

                
                return Json(new { sucesso = msgSuccess });
            }

            return Json(new { erro = "Erro inesperado." });
        }
    }

}
