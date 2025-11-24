using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace MeuProjetoMVC.Controllers
{
    [Route("/sistema/Entrega")]
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
        [HttpGet("DadosEntrega")]
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


        [HttpGet("CentralEntrega")]
        public IActionResult CentralEntrega()
        {

            return View();
        }

        [HttpGet("BuscarEnderecos")]
        public IActionResult BuscarEnderecos(string? retirada, string? situacao)
        {
            List<Entrega> entregas = new();
            List<EnderecoEntrega> endereco = new();
            List<Usuario> usuario = new();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                MySqlCommand cmd;

                if (retirada == "Entrega")
                {
                    cmd = new MySqlCommand(@"
                SELECT e.codEntrega, e.Numero, e.Complemento, e.TipoEndereco,
                       e.codUsuario, e.dataInicial, e.dataFinal,
                       en.Cep, en.codEndereco, u.Nome, u.Email
                FROM Entrega e
                INNER JOIN Endereco_Entrega en ON e.codEnd = en.codEndereco
                INNER JOIN Usuario u ON e.codUsuario = u.codUsuario
                WHERE retirada = @retirada AND situacao = @situacao;
            ", conn);
                }
                else
                {
                    cmd = new MySqlCommand(@"
                SELECT e.codEntrega, e.dataInicial, e.dataFinal,
                       e.codUsuario, u.Nome, u.Email
                FROM Entrega e
                INNER JOIN Endereco_Entrega en ON e.codEnd = en.codEndereco
                INNER JOIN Usuario u ON e.codUsuario = u.codUsuario
                WHERE retirada = @retirada AND situacao = @situacao;
            ", conn);
                }

                cmd.Parameters.AddWithValue("@retirada", retirada);
                cmd.Parameters.AddWithValue("@situacao", situacao);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    entregas.Add(new Entrega
                    {
                        codEntrega = rd["codEntrega"] as int? ?? 0,
                        Numero = rd["Numero"] as string ?? "",
                        Complemento = rd["Complemento"] as string ?? "",
                        tipoEndereco = rd["TipoEndereco"] as string ?? "",
                        dataInicial = rd.IsDBNull("dataInicial") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataInicial")),
                        dataFinal = rd.IsDBNull("dataFinal") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataFinal")),
                    });

                    if (!rd.IsDBNull("codEndereco"))
                    {
                        endereco.Add(new EnderecoEntrega
                        {
                            codEndereco = rd.GetInt32("codEndereco"),
                            Cep = rd["Cep"] as string ?? ""
                        });
                    }

                    usuario.Add(new Usuario
                    {
                        CodUsuario = rd.GetInt32("codUsuario"),
                        Nome = rd["Nome"] as string ?? "",
                        Email = rd["Email"] as string ?? ""
                    });
                }

                return Json(new
                {
                    sucesso = true,
                    entregas,
                    endereco,
                    usuario
                });
            }
            catch (MySqlException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }

        }





            // ==================================
            // SALVA DADOS DO ENDEREÇO
            // ==================================
            [HttpPost("DadosCliente")]
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
        [HttpPost("Destinatario")]
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