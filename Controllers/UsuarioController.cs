using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using System;
using System.Collections.Generic;
using BCrypt.Net;

namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize(RoleAnyOf = "Admin")] // Somente Admin
    public class UsuariosController : Controller
    {
        private readonly string _connectionString;

        public UsuariosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ==========================================================
        // LISTAR USUÁRIOS ATIVOS
        // ==========================================================
        public IActionResult Index()
        {
            var usuarios = new List<Usuario>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("CALL sp_usuario_listar_ativos();", conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    usuarios.Add(new Usuario
                    {
                        CodUsuario = reader.GetInt32("codUsuario"),
                        Role = reader["Role"]?.ToString() ?? "",
                        Nome = reader["Nome"]?.ToString() ?? "",
                        Email = reader["Email"]?.ToString() ?? "",
                        Ativo = reader["Ativo"]?.ToString() ?? "S"
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao listar usuários: " + ex.Message;
            }

            return View(usuarios);
        }

        // ==========================================================
        // LISTAR USUÁRIOS INATIVOS
        // ==========================================================
        public IActionResult Inativos()
        {
            var usuarios = new List<Usuario>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("CALL sp_usuario_listar_inativos();", conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    usuarios.Add(new Usuario
                    {
                        CodUsuario = reader.GetInt32("codUsuario"),
                        Role = reader["Role"]?.ToString() ?? "",
                        Nome = reader["Nome"]?.ToString() ?? "",
                        Email = reader["Email"]?.ToString() ?? "",
                        Ativo = "N"
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao listar inativos: " + ex.Message;
            }

            return View(usuarios);
        }

        // ==========================================================
        // CRIAR USUÁRIO
        // ==========================================================
        public IActionResult Criar() => View(new Usuario());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(Usuario usuario)
        {
            if (!ModelState.IsValid)
                return View(usuario);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Verifica se e-mail já existe
                using (var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Usuario WHERE Email=@Email;", conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", usuario.Email);
                    var existe = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (existe > 0)
                    {
                        ModelState.AddModelError("Email", "Esse e-mail já está cadastrado.");
                        return View(usuario);
                    }
                }

                var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha, workFactor: 12);

                using var cmd = new MySqlCommand("CALL sp_usuario_criar(@role,@nome,@email,@senha,@ativo);", conn);
                cmd.Parameters.AddWithValue("@role", usuario.Role);
                cmd.Parameters.AddWithValue("@nome", usuario.Nome);
                cmd.Parameters.AddWithValue("@email", usuario.Email);
                cmd.Parameters.AddWithValue("@senha", senhaHash);
                cmd.Parameters.AddWithValue("@ativo", "S");
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao criar usuário: " + ex.Message;
                return View(usuario);
            }
        }

        // ==========================================================
        // EDITAR USUÁRIO
        // ==========================================================
        public IActionResult Editar(int id)
        {
            Usuario? usuario = null;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("CALL sp_usuario_buscar_por_id(@id);", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    usuario = new Usuario
                    {
                        CodUsuario = reader.GetInt32("codUsuario"),
                        Role = reader["Role"]?.ToString() ?? "",
                        Nome = reader["Nome"]?.ToString() ?? "",
                        Email = reader["Email"]?.ToString() ?? "",
                        Ativo = reader["Ativo"]?.ToString() ?? "S",
                        Senha = "",
                        ConfirmarSenha = ""
                    };
                }
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao buscar usuário: " + ex.Message;
            }

            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Usuario usuario)
        {
            // Remove obrigatoriedade de senha no editar
            ModelState.Remove("Senha");
            ModelState.Remove("ConfirmarSenha");

            if (!ModelState.IsValid)
                return View(usuario);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Verifica duplicidade de e-mail (exceto o próprio)
                using (var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM Usuario WHERE Email=@Email AND codUsuario<>@Id;", conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", usuario.Email);
                    checkCmd.Parameters.AddWithValue("@Id", usuario.CodUsuario);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        ModelState.AddModelError("Email", "Esse e-mail já está em uso por outro usuário.");
                        return View(usuario);
                    }
                }

                // Se senha nova foi informada, gera hash, senão mantém a antiga
                string senhaFinal = string.IsNullOrWhiteSpace(usuario.Senha)
                    ? GetSenhaAtual(usuario.CodUsuario)
                    : BCrypt.Net.BCrypt.HashPassword(usuario.Senha, workFactor: 12);

                using var cmd = new MySqlCommand(
                    "CALL sp_usuario_atualizar(@id,@role,@nome,@email,@senha,@ativo);", conn);
                cmd.Parameters.AddWithValue("@id", usuario.CodUsuario);
                cmd.Parameters.AddWithValue("@role", usuario.Role);
                cmd.Parameters.AddWithValue("@nome", usuario.Nome);
                cmd.Parameters.AddWithValue("@email", usuario.Email);
                cmd.Parameters.AddWithValue("@senha", senhaFinal);
                cmd.Parameters.AddWithValue("@ativo", usuario.Ativo ?? "S");
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao atualizar usuário: " + ex.Message;
                return View(usuario);
            }
        }

        // ==========================================================
        // EXCLUIR (DESATIVAR) USUÁRIO
        // ==========================================================
        [HttpGet]
        public IActionResult Excluir(int id)
        {
            Usuario? usuario = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("CALL sp_usuario_buscar_por_id(@id);", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                usuario = new Usuario
                {
                    CodUsuario = reader.GetInt32("codUsuario"),
                    Nome = reader["Nome"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    Role = reader["Role"]?.ToString() ?? "",
                    Ativo = reader["Ativo"]?.ToString() ?? "S"
                };
            }

            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public IActionResult ExcluirPost(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(
                    "CALL sp_usuario_atualizar_status(@p_id,@p_status);", conn);
                cmd.Parameters.AddWithValue("@p_id", id);
                cmd.Parameters.AddWithValue("@p_status", "N");
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário desativado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao desativar usuário: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================================
        // REATIVAR USUÁRIO
        // ==========================================================
        [HttpGet]
        public IActionResult Reativar(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(
                    "CALL sp_usuario_atualizar_status(@p_id,@p_status);", conn);
                cmd.Parameters.AddWithValue("@p_id", id);
                cmd.Parameters.AddWithValue("@p_status", "S");
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário reativado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao reativar usuário: " + ex.Message;
            }

            return RedirectToAction(nameof(Inativos));
        }

        // ==========================================================
        // Helper: Obter Senha Atual
        // ==========================================================
        private string GetSenhaAtual(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Senha FROM Usuario WHERE codUsuario=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
    }
}
