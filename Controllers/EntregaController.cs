using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Components.Forms.Mapping;
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
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                MySqlCommand cmd;

                // ================================
                //     CASO 1 → RETIRADA = ENTREGA
                // ================================
                if (retirada == "Entrega")
                {
                    cmd = new MySqlCommand(@"
                SELECT e.codEntrega, e.Numero, e.Complemento, e.TipoEndereco,
                       e.codUsuario, e.dataInicial, e.dataFinal, e.situacao,
                       en.Cep, en.codEndereco, 
                       u.Nome, u.Email
                FROM Entrega e
                LEFT JOIN Endereco_Entrega en ON e.codEnd = en.codEndereco
                INNER JOIN Usuario u ON e.codUsuario = u.codUsuario
                WHERE e.retirada = @retirada AND e.situacao = @situacao;
            ", conn);

                    cmd.Parameters.AddWithValue("@retirada", retirada);
                    cmd.Parameters.AddWithValue("@situacao", situacao);

                    List<Entrega> entregas = new();
                    List<EnderecoEntrega> enderecos = new();
                    List<Usuario> usuarios = new();

                    var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        entregas.Add(new Entrega
                        {
                            codEntrega = rd.GetInt32("codEntrega"),
                            Situacao = rd["situacao"]?.ToString(),
                            Numero = rd["Numero"]?.ToString(),
                            Complemento = rd["Complemento"]?.ToString(),
                            tipoEndereco = rd["TipoEndereco"]?.ToString(),
                            dataInicial = rd.IsDBNull("dataInicial") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataInicial")),
                            dataFinal = rd.IsDBNull("dataFinal") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataFinal"))
                        });

                        // Endereço só existe no modo "Entrega"
                        if (!rd.IsDBNull("codEndereco"))
                        {
                            enderecos.Add(new EnderecoEntrega
                            {
                                codEndereco = rd.GetInt32("codEndereco"),
                                Cep = rd["Cep"]?.ToString()
                            });
                        }

                        usuarios.Add(new Usuario
                        {
                            CodUsuario = rd.GetInt32("codUsuario"),
                            Nome = rd["Nome"]?.ToString(),
                            Email = rd["Email"]?.ToString()
                        });
                    }

                    return Json(new
                    {
                        sucesso = true,
                        retirada = "Entrega",
                        entregas,
                        enderecos,
                        usuarios
                    });
                }

                // ================================
                //     CASO 2 → RETIRADA = LOCAL
                // ================================
                else
                {
                    cmd = new MySqlCommand(@"
                      SELECT e.codEntrega, e.dataInicial, e.dataFinal, e.situacao,
                       e.codUsuario,
                       u.Nome, u.Email
                      FROM Entrega e
                      INNER JOIN Usuario u ON e.codUsuario = u.codUsuario
                      WHERE e.retirada = @retirada AND e.Situacao = @situacao;
                    ", conn);

                    cmd.Parameters.AddWithValue("@retirada", retirada);
                    cmd.Parameters.AddWithValue("@situacao", situacao);

                    List<Entrega> entregas = new();
                    List<Usuario> usuarios = new();

                    var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        entregas.Add(new Entrega
                        {
                            Situacao = rd["situacao"]?.ToString(),
                            codEntrega = rd.GetInt32("codEntrega"),
                            dataInicial = rd.IsDBNull("dataInicial") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataInicial")),
                            dataFinal = rd.IsDBNull("dataFinal") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataFinal"))
                        });

                        usuarios.Add(new Usuario
                        {
                            CodUsuario = rd.GetInt32("codUsuario"),
                            Nome = rd["Nome"]?.ToString(),
                            Email = rd["Email"]?.ToString()
                        });
                    }

                    return Json(new
                    {
                        sucesso = true,
                        retirada = "Local",
                        entregas,
                        usuarios
                    });
                }
            }
            catch (MySqlException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }


        [HttpGet("BuscarEnderecosPorNome")]
        public IActionResult BuscarEnderecosPorNome(string? retirada, string? situacao, string? nomeCliente)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                MySqlCommand cmd;

                // ================================
                //     CASO 1 → RETIRADA = ENTREGA
                // ================================
                if (retirada == "Entrega")
                {
                    cmd = new MySqlCommand(@"
                SELECT e.codEntrega, e.Numero, e.Complemento, e.TipoEndereco,
                       e.codUsuario, e.dataInicial, e.dataFinal, e.situacao,
                       en.Cep, en.codEndereco, 
                       u.Nome, u.Email
                FROM Entrega e
                LEFT JOIN Endereco_Entrega en ON e.codEnd = en.codEndereco
                INNER JOIN Usuario u ON e.codUsuario = u.codUsuario
                WHERE e.retirada = @retirada AND e.situacao = @situacao AND (@nome = '' OR u.Nome LIKE CONCAT('%', @nome, '%'));
            ", conn);

                    cmd.Parameters.AddWithValue("@retirada", retirada);
                    cmd.Parameters.AddWithValue("@situacao", situacao);
                    cmd.Parameters.AddWithValue("@nome", nomeCliente);
                    List<Entrega> entregas = new();
                    List<EnderecoEntrega> enderecos = new();
                    List<Usuario> usuarios = new();

                    var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        entregas.Add(new Entrega
                        {
                            codEntrega = rd.GetInt32("codEntrega"),
                            Situacao = rd["situacao"]?.ToString(),
                            Numero = rd["Numero"]?.ToString(),
                            Complemento = rd["Complemento"]?.ToString(),
                            tipoEndereco = rd["TipoEndereco"]?.ToString(),
                            dataInicial = rd.IsDBNull("dataInicial") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataInicial")),
                            dataFinal = rd.IsDBNull("dataFinal") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataFinal"))
                        });

                        // Endereço só existe no modo "Entrega"
                        if (!rd.IsDBNull("codEndereco"))
                        {
                            enderecos.Add(new EnderecoEntrega
                            {
                                codEndereco = rd.GetInt32("codEndereco"),
                                Cep = rd["Cep"]?.ToString()
                            });
                        }

                        usuarios.Add(new Usuario
                        {
                            CodUsuario = rd.GetInt32("codUsuario"),
                            Nome = rd["Nome"]?.ToString(),
                            Email = rd["Email"]?.ToString()
                        });
                    }

                    return Json(new
                    {
                        sucesso = true,
                        retirada = "Entrega",
                        entregas,
                        enderecos,
                        usuarios
                    });
                }

                // ================================
                //     CASO 2 → RETIRADA = LOCAL
                // ================================
                else
                {
                    cmd = new MySqlCommand(@"
                      SELECT e.codEntrega, e.dataInicial, e.dataFinal, e.situacao,
                       e.codUsuario,
                       u.Nome, u.Email
                      FROM Entrega e
                      INNER JOIN Usuario u ON e.codUsuario = u.codUsuario
                      WHERE e.retirada = @retirada AND e.Situacao = @situacao And (@nome = '' OR u.Nome LIKE CONCAT('%', @nome, '%'));
                    ", conn);

                    cmd.Parameters.AddWithValue("@retirada", retirada);
                    cmd.Parameters.AddWithValue("@situacao", situacao);
                    cmd.Parameters.AddWithValue("@nome", nomeCliente);

                    List<Entrega> entregas = new();
                    List<Usuario> usuarios = new();

                    var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        entregas.Add(new Entrega
                        {
                            Situacao = rd["situacao"]?.ToString(),
                            codEntrega = rd.GetInt32("codEntrega"),
                            dataInicial = rd.IsDBNull("dataInicial") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataInicial")),
                            dataFinal = rd.IsDBNull("dataFinal") ? default : DateOnly.FromDateTime(rd.GetDateTime("dataFinal"))
                        });

                        usuarios.Add(new Usuario
                        {
                            CodUsuario = rd.GetInt32("codUsuario"),
                            Nome = rd["Nome"]?.ToString(),
                            Email = rd["Email"]?.ToString()
                        });
                    }

                    return Json(new
                    {
                        sucesso = true,
                        retirada = "Local",
                        entregas,
                        usuarios
                    });
                }
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

        [HttpGet("Detalhes")]
        public IActionResult Detalhes(int codEntrega, string tipoRetirada, string TipoEndereco)
        {
            try
            {
                var model = new Detalhes();
                var conn = new MySqlConnection(_connectionString);
                conn.Open();

                if (tipoRetirada == "Entrega")
                {
                    if (TipoEndereco == "Apartamento")
                    {
                        // SELECT COMPLETO PARA APARTAMENTO
                        using (var cmd = new MySqlCommand(@"
                        select e.codEntrega, e.codUsuario, e.valorTotal, e.codEnd, 
                        e.Numero, e.Complemento, e.TipoEndereco, e.andar, e.NomePredio,
                        e.nomeDestinatario, e.emailDestinatario,
                        en.Cep, en.Logradouro, en.Estado, en.Bairro, en.Cidade,
                        u.Nome, u.Email, u.Telefone,
                        ep.nomeProduto, ep.Quantidade, ep.Valor
                        from Entrega e
                        left join endereco_entrega en on e.codEnd = en.codEndereco
                        inner join entrega_produto ep on e.codEntrega = ep.codEntrega
                        inner join usuario u on e.codUsuario = u.codUsuario
                        where e.codEntrega = @cod;
                        ", conn))
                        {
                            cmd.Parameters.AddWithValue("@cod", codEntrega);
                            var rd = cmd.ExecuteReader();

                            if (rd.Read())
                            {
                                model.codUsuario = rd["codUsuario"] as int?;
                                model.valorTotal = rd["valorTotal"] as decimal?;
                                model.Numero = rd["Numero"]?.ToString();
                                model.Complemento = rd["Complemento"]?.ToString();
                                model.TipoEndereco = rd["TipoEndereco"]?.ToString();
                                model.Andar = rd["andar"]?.ToString();
                                model.NomePredio = rd["NomePredio"]?.ToString();
                                model.nomeDestinatario = rd["nomeDestinatario"]?.ToString();
                                model.emailDestinatario = rd["emailDestinatario"]?.ToString();
                                model.Cep = rd["Cep"]?.ToString();
                                model.Logradouro = rd["Logradouro"]?.ToString();
                                model.Estado = rd["Estado"]?.ToString();
                                model.Bairro = rd["Bairro"]?.ToString();
                                model.Cidade = rd["Cidade"]?.ToString();
                                model.Nome = rd["Nome"]?.ToString();
                                model.Email = rd["Email"]?.ToString();
                                model.Telefone = rd["Telefone"]?.ToString();
                                model.nomeProduto = rd["nomeProduto"]?.ToString();
                                model.Quantidade = rd["Quantidade"]?.ToString();
                                model.Valor = rd["Valor"]?.ToString();
                            }
                        }
                    }
                    else
                    {
                        // SELECT SEM CAMPOS DE APARTAMENTO
                        using (var cmd = new MySqlCommand(@"
                    select e.codEntrega, e.codUsuario, e.valorTotal, e.codEnd,
                    e.Numero, e.Complemento, e.TipoEndereco,
                    e.nomeDestinatario, e.emailDestinatario,
                    en.Cep, en.Logradouro, en.Estado, en.Bairro, en.Cidade,
                    u.Nome, u.Email, u.Telefone,
                    ep.nomeProduto, ep.Quantidade, ep.Valor
                    from Entrega e
                    left join endereco_entrega en on e.codEnd = en.codEndereco
                    inner join entrega_produto ep on e.codEntrega = ep.codEntrega
                    inner join usuario u on e.codUsuario = u.codUsuario
                    where e.codEntrega = @cod;
                ", conn))
                        {
                            cmd.Parameters.AddWithValue("@cod", codEntrega);
                            var rd = cmd.ExecuteReader();

                            if (rd.Read())
                            {
                                // mesmos campos, exceto andar e NomePredio
                                model.codUsuario = rd["codUsuario"] as int?;
                                model.valorTotal = rd["valorTotal"] as decimal?;
                                model.Numero = rd["Numero"]?.ToString();
                                model.Complemento = rd["Complemento"]?.ToString();
                                model.TipoEndereco = rd["TipoEndereco"]?.ToString();
                                model.nomeDestinatario = rd["nomeDestinatario"]?.ToString();
                                model.emailDestinatario = rd["emailDestinatario"]?.ToString();
                                model.Cep = rd["Cep"]?.ToString();
                                model.Logradouro = rd["Logradouro"]?.ToString();
                                model.Estado = rd["Estado"]?.ToString();
                                model.Bairro = rd["Bairro"]?.ToString();
                                model.Cidade = rd["Cidade"]?.ToString();
                                model.Nome = rd["Nome"]?.ToString();
                                model.Email = rd["Email"]?.ToString();
                                model.Telefone = rd["Telefone"]?.ToString();
                                model.nomeProduto = rd["nomeProduto"]?.ToString();
                                model.Quantidade = rd["Quantidade"]?.ToString();
                                model.Valor = rd["Valor"]?.ToString();
                            }
                        }
                    }
                }
                else // LOCAL
                {
                    using (var cmd = new MySqlCommand(@"
                select e.codEntrega, e.codUsuario, e.valorTotal,
                u.Nome, u.Email, u.Telefone,
                ep.nomeProduto, ep.Quantidade, ep.Valor
                from Entrega e
                inner join entrega_produto ep on e.codEntrega = ep.codEntrega
                inner join usuario u on e.codUsuario = u.codUsuario
                where e.codEntrega = @cod;
            ", conn))
                    {
                        cmd.Parameters.AddWithValue("@cod", codEntrega);
                        var rd = cmd.ExecuteReader();

                        while (rd.Read())
                        {
                            model.codUsuario = rd["codUsuario"] as int?;
                            model.valorTotal = rd["valorTotal"] as decimal?;
                            model.Nome = rd["Nome"]?.ToString();
                            model.Email = rd["Email"]?.ToString();
                            model.Telefone = rd["Telefone"]?.ToString();
                            model.nomeProduto = rd["nomeProduto"]?.ToString();
                            model.Quantidade = rd["Quantidade"]?.ToString();
                            model.Valor = rd["Valor"]?.ToString();
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                return BadRequest("Erro interno: " + ex.Message);
            }
        }


        [HttpPost("Acaminho")]
        public IActionResult AdicionarACaminho([FromBody]Entrega entrega)
        {
            if( entrega.codEntrega == 0)
            {
                return Json(new {mensagem ="Código da entrega inválido", sucesso = false });
            }
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();
                var cmd = new MySqlCommand("AcaminhoEntrega", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("c_codentrega", entrega.codEntrega);

                cmd.ExecuteNonQuery();

                return Json(new { mensagem = "Pedido foi redirecionado ao setor de pedidos encaminhados", sucesso = true });

            }
            catch(MySqlException ex)
            {
                return Json(new
                {
                    mensagem = "Erro ao realizar a conexão:" + ex.Message,
                    sucesso = false
                });
            }

        }

        [HttpPost("Em_andamento_Entrega")]
        public IActionResult EmAndamentoEntrega([FromBody]Entrega entrega)
        {
            if (entrega.codEntrega == 0)
            {
                return Json(new {mensagem = "Código da entrega inválida", sucesso = false});
            }
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();
                var cmd = new MySqlCommand("Em_andamentoEntrega", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("c_codentrega", entrega.codEntrega);

                cmd.ExecuteNonQuery();

                return Json(new { mensagem = "Pedido foi redirecionado ao setor de pedidos encaminhados", sucesso = true });

            }
            catch (MySqlException ex)
            {
                return Json(new { mensagem = "Erro ao realizar conexão:" + ex.Message, sucesso = false });
            }

        }



        [HttpPost("Finalizar")]
        public IActionResult FinalizarEntrega([FromBody]Entrega entrega)
        {
            if (entrega.codEntrega == 0)
            {
                return Json(new { mensagem = "Código da entrega inválido", sucesso =false });
            }
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();
                var cmd = new MySqlCommand("FinalizadaEntrega", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("c_codentrega", entrega.codEntrega);

                cmd.ExecuteNonQuery();

                return Json(new { mensagem = "Pedido foi finalizado com sucesso", sucesso = true });

            }
            catch (MySqlException ex)
            {
                return BadRequest(new { mensagem = "Erro ao realizar a coneção. Erro:" + ex.Message, sucesso = false });
            }

        }

        [HttpPost("FinalizarLocal")]
        public IActionResult FinalizarLocal([FromBody] Entrega entrega)
        {
            if (entrega.codEntrega == 0)
            {
                return Json(new{ mensagem ="Código da entrega inválido", sucesso = false });
            }
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();
                var cmd = new MySqlCommand("FinalizadaLocal", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("c_codentrega", entrega.codEntrega);

                cmd.ExecuteNonQuery();

                return Json(new { mensagem = "Pedido foi retirado pelo cliente com sucesso", sucesso = true });

            }
            catch (MySqlException ex)
            {
                return Json(new { mensagem = "Erro ao realizar a coneção. Erro:" + ex.Message, sucesso = false });
            }

        }


    }


}