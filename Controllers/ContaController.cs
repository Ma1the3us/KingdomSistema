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

        [HttpPost]
        public IActionResult Registrar(Usuario model, string confirmarSenha)
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
                var ativo = reader["Ativo"]?.ToString() ?? "S";
                var codUsuario = Convert.ToInt32(reader["codUsuario"]);
                reader.Close();

                if (ativo == "S")
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
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha);

            // Insere novo usuário
            using var cmd = new MySqlCommand("CALL sp_usuario_criar(@role,@nome,@email,@senha,@ativo)", conn);
            cmd.Parameters.AddWithValue("@role", "Cliente");
            cmd.Parameters.AddWithValue("@nome", model.Nome);
            cmd.Parameters.AddWithValue("@email", model.Email);
            cmd.Parameters.AddWithValue("@senha", senhaHash);
            cmd.Parameters.AddWithValue("@ativo", "S");
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
            cmd.Parameters.AddWithValue("@status", "S");
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
            cmd.Parameters.AddWithValue("@status", "N");
            cmd.ExecuteNonQuery();

            HttpContext.Session.Clear();
            TempData["Sucesso"] = "Conta desativada com sucesso.";
            return RedirectToAction("Login", "Auth");
        }
    }
}
