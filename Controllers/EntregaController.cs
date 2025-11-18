using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace MeuProjetoMVC.Controllers
{
    public class EntregaController : Controller
    {
        private readonly string _connectionString;

        public EntregaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // =============================
        // EXIBE A PÁGINA DE ENTREGA
        // =============================
        public IActionResult DadosEntrega(int codVenda, string retirada)
        {
            EnderecoEntrega entrega = null;

            if (TempData["Endereco"] != null)
            {
                entrega = JsonSerializer.Deserialize<EnderecoEntrega>(TempData["Endereco"].ToString());
            }

            ViewBag.Endereco = entrega;

            return View();
        }

        // ==================================
        // SALVA DADOS DO ENDEREÇO
        // ==================================
        [HttpPost]
        public IActionResult DadosCliente([FromBody] DadosEntregaDTO dto)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (user == null || user == 0)
                return Unauthorized();

            var endreq = dto.Endereco;
            var req = dto.Entrega;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("atualizar_dados_entrega_cliente", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("u_cod", user);
                cmd.Parameters.AddWithValue("v_cod", req.codVenda);
                cmd.Parameters.AddWithValue("p_cep", endreq.Cep);
                cmd.Parameters.AddWithValue("p_numero", req.Numero);
                cmd.Parameters.AddWithValue("p_complemento", req.Complemento);
                cmd.Parameters.AddWithValue("p_tipoEndereco", req.tipoEndereco);
                cmd.Parameters.AddWithValue("p_andar", req.Andar);
                cmd.Parameters.AddWithValue("p_nomePredio", req.NomePredio);
                cmd.Parameters.AddWithValue("p_logradouro", endreq.Logradouro);
                cmd.Parameters.AddWithValue("p_estado", endreq.Estado);
                cmd.Parameters.AddWithValue("p_bairro", endreq.Bairro);
                cmd.Parameters.AddWithValue("p_cidade", endreq.Cidade);

                cmd.ExecuteNonQuery();

                return Json(new { sucesso = true, mensagem = "Dados salvos!" });
            }
            catch (MySqlException ex)
            {
                return Json(new { sucesso = false, mensagem = ex.Message });
            }
        }

        // ==================================
        // SALVA DESTINATÁRIO
        // ==================================
        [HttpPost]
        public IActionResult Destinatario([FromBody] Entrega req)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (user == null || user == 0)
                return Unauthorized();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("atualizar_Destinatario", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("u_cod", user);
                cmd.Parameters.AddWithValue("v_cod", req.codVenda);
                cmd.Parameters.AddWithValue("e_nome", req.nomeDestinatario);
                cmd.Parameters.AddWithValue("e_email", req.emailDestinatario);

                cmd.ExecuteNonQuery();

                return Json(new { sucesso = true, mensagem = "Destinatário salvo!" });
            }
            catch (MySqlException ex)
            {
                return Json(new { sucesso = false, mensagem = ex.Message });
            }
        }
    }
}