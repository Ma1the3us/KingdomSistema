using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using System;

namespace MeuProjetoMVC.Controllers
{
    public class ContaController : Controller
    {
        private readonly string _connectionString;

        public ContaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ========================
        // Registro de Cliente
        // ========================
        [HttpGet]
        public IActionResult Registrar()
        {
            return View(new Usuario());
        }
    
         [HttpGet]
        public IActionResult AdicionarCartao()
        {
            return View(new cartaoCli());
        }
        
        [HttpPost]
        public IActionResult Registrar(Usuario model, string confirmarSenha, IFormFile capa)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Senha != confirmarSenha)
            {
                ViewBag.Erro = "As senhas não coincidem.";
                return View(model);
            }



            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Verifica se já existe usuário com este email
            using var cmdCheck = new MySqlCommand("SELECT codUsuario, Ativo FROM Usuario WHERE Email=@Email", conn);
            cmdCheck.Parameters.AddWithValue("@Email", model.Email);
            using var reader = cmdCheck.ExecuteReader();
            if (reader.Read())
            {
                var ativo = reader["Ativo"]?.ToString() ?? "1";
                var codUsuario = Convert.ToInt32(reader["codUsuario"]);
                reader.Close();

                if (ativo == "1")
                {
                    ViewBag.Erro = "Já existe um usuário ativo com este e-mail.";
                    return View(model);
                }
                else
                {
                    ViewBag.ReativarId = codUsuario;
                    return View("ReativarConta", model);
                }
            }
            reader.Close();

            // Criptografa senha


            // Insere novo usuário
            if (capa != null && capa.Length > 0)
            {
                using var ms = new MemoryStream();
                capa.CopyTo(ms);
                model.Imagens = ms.ToArray();
            }


            var senhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha);

            using var cmd = new MySqlCommand("cadastrar_usuario", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_role", "Cliente");
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_email", model.Email);
            cmd.Parameters.AddWithValue("p_senha", senhaHash);
            cmd.Parameters.AddWithValue("p_foto", model.Imagens);
            cmd.Parameters.AddWithValue("p_telefone", model.Telefone);
            cmd.ExecuteNonQuery();

            TempData["Sucesso"] = "Conta criada com sucesso! Faça login.";
            return RedirectToAction("Login", "Auth");
        }

        // ========================
        // Reativar conta inativa
        // ========================
        [HttpPost]
        public IActionResult ReativarConta(int codUsuario)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("CALL sp_usuario_atualizar_status(@id,@status)", conn);
            cmd.Parameters.AddWithValue("@id", codUsuario);
            cmd.Parameters.AddWithValue("@status", "1");
            cmd.ExecuteNonQuery();

            TempData["Sucesso"] = "Conta reativada com sucesso! Faça login.";
            return RedirectToAction("Login", "Auth");
        }

        // ========================
        // Editar Conta
        // ========================
        [HttpGet]
        public IActionResult Editar()
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            Usuario? model = null;
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("CALL sp_usuario_buscar_por_id(@id)", conn);
            cmd.Parameters.AddWithValue("@id", codUsuario.Value);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model = new Usuario
                {
                    CodUsuario = Convert.ToInt32(reader["codUsuario"]),
                    Nome = reader["Nome"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    Role = reader["Role"]?.ToString() ?? "Cliente"
                };
            }

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost]
        public IActionResult Editar(Usuario model)
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Verifica se o novo email já pertence a outro usuário
            using (var cmdVerifica = new MySqlCommand(
                "SELECT COUNT(*) FROM Usuario WHERE Email=@Email AND codUsuario <> @Id", conn))
            {
                cmdVerifica.Parameters.AddWithValue("@Email", model.Email);
                cmdVerifica.Parameters.AddWithValue("@Id", codUsuario.Value);
                long existe = Convert.ToInt64(cmdVerifica.ExecuteScalar());
                if (existe > 0)
                {
                    ViewBag.Erro = "Este e-mail já está sendo utilizado por outro usuário.";
                    return View(model);
                }
            }

            // Busca senha atual
            string? senhaAtual = null;
            using (var cmdBusca = new MySqlCommand("SELECT Senha FROM Usuario WHERE codUsuario=@id", conn))
            {
                cmdBusca.Parameters.AddWithValue("@id", codUsuario.Value);
                senhaAtual = cmdBusca.ExecuteScalar()?.ToString();
            }

            // Se o usuário digitou uma nova senha, atualiza. Caso contrário, mantém a antiga
            var senhaParaSalvar = !string.IsNullOrWhiteSpace(model.Senha)
                ? BCrypt.Net.BCrypt.HashPassword(model.Senha)
                : senhaAtual;

            // Atualiza dados
            // Colega... Tem mais coisas para atualizar
            using (var cmd = new MySqlCommand(
                "UPDATE Usuario SET Nome=@Nome, Email=@Email, Senha=@Senha WHERE codUsuario=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Nome", model.Nome);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Senha", senhaParaSalvar);
                cmd.Parameters.AddWithValue("@Id", codUsuario.Value);
                cmd.ExecuteNonQuery();
            }

            TempData["Sucesso"] = "Seus dados foram atualizados com sucesso!";
            return RedirectToAction("Editar");
        }

        // ========================
        // Desativar Conta
        // ========================
        [HttpGet]
        public IActionResult Desativar()
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        [HttpPost]
        public IActionResult DesativarConfirmado()
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("CALL sp_usuario_atualizar_status(@id,@status)", conn);
            cmd.Parameters.AddWithValue("@id", codUsuario.Value);
            cmd.Parameters.AddWithValue("@status", "0");
            cmd.ExecuteNonQuery();

            HttpContext.Session.Clear();
            TempData["Sucesso"] = "Conta desativada com sucesso.";
            return RedirectToAction("Login", "Auth");
        }
   

        // ========================
        // Método para visualizar perfil
        // ========================
        [HttpGet]
public IActionResult Perfil()
{
    var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
    if (codUsuario == null)
        return RedirectToAction("Login", "Auth");

    Usuario model = null;

    using (var conn = new MySqlConnection(_connectionString))
    {
        conn.Open();

        // ------------------------------------------------
        // 1. BUSCA OS DADOS DO USUÁRIO
        // ------------------------------------------------
        using (var cmd = new MySqlCommand("CALL sp_usuario_buscar_por_id(@id)", conn))
        {
            cmd.Parameters.AddWithValue("@id", codUsuario.Value);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    model = new Usuario
                    {
                        CodUsuario = Convert.ToInt32(reader["codUsuario"]),
                        Nome = reader["Nome"]?.ToString() ?? "",
                        Email = reader["Email"]?.ToString() ?? "",
                        Role = reader["Role"]?.ToString() ?? "Cliente",
                        Telefone = reader["Telefone"]?.ToString(),
                        Imagens = reader["Foto"] as byte[]
                    };
                }
            }
        }

        if (model == null)
            return NotFound();

        // ------------------------------------------------
        // 2. BUSCA OS CARTÕES DO USUÁRIO
        // ------------------------------------------------
        model.CartaoCli = new List<cartaoCli>();
        using (var cmd = new MySqlCommand("SELECT * FROM Cartao_Clie WHERE codUsuario = @id", conn))
        {
            cmd.Parameters.AddWithValue("@id", codUsuario.Value);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    model.CartaoCli.Add(new cartaoCli
                    {
                        codCart = Convert.ToInt32(reader["codCart"]),
                        Numero = reader["Numero"]?.ToString(),
                        digitos = reader["digitos"]?.ToString(),
                        bandeira = reader["bandeira"]?.ToString(),
                        tipoCart = reader["tipoCart"]?.ToString()
                    });
                }
            }
        }

        // ------------------------------------------------
        // 3. BUSCA HISTÓRICO DE VENDAS
        // ------------------------------------------------
        model.Vendas = new List<Venda>();
        using (var cmd = new MySqlCommand(
            "SELECT codVenda, valorTotalVenda FROM Venda WHERE codUsuario=@id", conn))
        {
            cmd.Parameters.AddWithValue("@id", codUsuario.Value);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    model.Vendas.Add(new Venda
                    {
                        codVenda = Convert.ToInt32(reader["codVenda"]),
                        valorTotalVenda = Convert.ToDecimal(reader["valorTotalVenda"])
                    });
                }
            }
        }
    }

    return View(model);
}


  
        // ========================
        // Métodos para Cartões (Listar, Excluir e Adicionar)
        // ========================
        [HttpGet]
        public IActionResult Cartoes()
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            List<cartaoCli> cartoes = new List<cartaoCli>();
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT * FROM Cartao_Clie WHERE codUsuario = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", codUsuario.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cartoes.Add(new cartaoCli
                            {
                                codCart = Convert.ToInt32(reader["codCart"]),
                                Numero = reader["Numero"]?.ToString(),
                                digitos = reader["digitos"]?.ToString(),
                                bandeira = reader["bandeira"]?.ToString(),
                                tipoCart = reader["tipoCart"]?.ToString()
                            });
                        }
                    }
                }
            }

            return View(cartoes);
        }


        [HttpPost]
        public IActionResult AdicionarCartao(cartaoCli cartao)
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("INSERT INTO Cartao_Clie (Numero, digitos, bandeira, tipoCart, codUsuario) VALUES (@Numero, @digitos, @bandeira, @tipoCart, @codUsuario)", conn))
                {
                    cmd.Parameters.AddWithValue("@Numero", cartao.Numero);
                    cmd.Parameters.AddWithValue("@digitos", cartao.digitos);
                    cmd.Parameters.AddWithValue("@bandeira", cartao.bandeira);
                    cmd.Parameters.AddWithValue("@tipoCart", cartao.tipoCart);
                    cmd.Parameters.AddWithValue("@codUsuario", codUsuario.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Sucesso"] = "Cartão cadastrado com sucesso!";
            return RedirectToAction("Cartoes");
        }

        [HttpPost]
        public IActionResult ExcluirCartao(int codCart)
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM Cartao_Clie WHERE codCart = @codCart AND codUsuario = @codUsuario", conn))
                {
                    cmd.Parameters.AddWithValue("@codCart", codCart);
                    cmd.Parameters.AddWithValue("@codUsuario", codUsuario.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Sucesso"] = "Cartão excluído com sucesso!";
            return RedirectToAction("Cartoes");
        }

        // ========================
        // Histórico de Vendas
        // ========================
        [HttpGet]
        public IActionResult HistoricoVendas()
        {
            var codUsuario = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (codUsuario == null)
                return RedirectToAction("Login", "Auth");

            List<Venda> vendas = new List<Venda>();
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT v.codVenda, v.valorTotalVenda, v.formaPag, v.situacao, v.dataE FROM Venda v LEFT JOIN Entrega e ON v.codVenda = e.codVenda WHERE v.codUsuario = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", codUsuario.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vendas.Add(new Venda
                            {
                                codVenda = Convert.ToInt32(reader["codVenda"]),
                                valorTotalVenda = Convert.ToDecimal(reader["valorTotalVenda"]),
                                formaPag = reader["formaPag"]?.ToString(),
                                situacao = reader["situacao"]?.ToString(),
                                dataE = Convert.ToDateTime(reader["dataE"]),
                                
                            });
                        }
                    }
                }
            }

            return View(vendas);
        }

        // ========================
        // Detalhes da Venda
        // ========================
        [HttpGet]
        public IActionResult DetalhesVenda(int codEntrega)
        {
            // Reutiliza o método Detalhes de Entrega que já existe, só adapta
            return RedirectToAction("Detalhes", "Entrega", new { codEntrega });
        }
        
    }
}
