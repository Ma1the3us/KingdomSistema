using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Ocsp;

namespace MeuProjetoMVC.Controllers
{
    public class EntregaController : Controller
    {
        private readonly string _connectionString;

        EntregaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DadosEntrega(int? codVenda, string? retirada)
        {

            return View();
        }

        public IActionResult DadosCliente([FromBody] EnderecoEntrega endreq, Entrega req)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (user == null || user == 0)
                return Unauthorized();


            if(req.tipoEndereco == "Casa")
            {
                req.Andar = null;
                req.NomePredio = null;
            }
            
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("atualizar_dados_entrega_cliente", conn) { CommandType = System.Data.CommandType.StoredProcedure };
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

                return Json(new
                {
                    sucesso = true,
                    mensagem = "Dados registrados com sucesso",
                    P = 1
                });
            
            }
            catch (MySqlException ex)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Erro ao realizar o registro de dados. Execessão:" + ex,
                    p = 0
                });
            }
        }

        public IActionResult Destinatario([FromBody] Entrega req) 
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (user == null || user == 0)
                return Unauthorized();

            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("atualizar_Destinatario", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("u_cod", user);
                cmd.Parameters.AddWithValue("v_cod", req.codVenda);
                cmd.Parameters.AddWithValue("e_nome", req.nomeDestinatario);
                cmd.Parameters.AddWithValue("v_cod", req.emailDestinatario);

                cmd.ExecuteNonQuery();

                return Json(new
                {
                    sucesso = true,
                    mensagem = "Dados registrados com sucesso",
                    P = 2
                });

            }
            catch (MySqlException ex)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Erro ao realizar o registro de dados. Execessão:" + ex,
                    p = 0
                });
            }
        }

    }
}
