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
    [SessionAuthorize(RoleAnyOf = "Admin")] // apenas Admin pode gerenciar
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
                        Role = reader["Role"]?.ToString() ?? string.Empty,
                        Nome = reader["Nome"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
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
                        Role = reader["Role"]?.ToString() ?? string.Empty,
                        Nome = reader["Nome"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        Ativo = reader["Ativo"]?.ToString() ?? "N"
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao listar inativos: " + ex.Message;
            }

            return View("Inativos", usuarios);
        }

        // ==========================================================
        // CRIAR USUÁRIO
        // ==========================================================
        public IActionResult Criar() => View(new Usuario());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(Usuario usuario)
        {
            if (!ModelState.IsValid) return View(usuario);

            try
            {
                var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha, workFactor: 12);

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
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
                        Role = reader["Role"]?.ToString() ?? string.Empty,
                        Nome = reader["Nome"]?.ToString() ?? string.Empty,
                        Email = reader["Email"]?.ToString() ?? string.Empty,
                        Ativo = reader["Ativo"]?.ToString() ?? "S",
                        Senha = ""
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
            if (!ModelState.IsValid) return View(usuario);

            try
            {
                var senhaParaSalvar = string.IsNullOrWhiteSpace(usuario.Senha)
                    ? GetSenhaAtual(usuario.CodUsuario)
                    : BCrypt.Net.BCrypt.HashPassword(usuario.Senha, workFactor: 12);

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("CALL sp_usuario_atualizar(@id,@role,@nome,@email,@senha,@ativo);", conn);
                cmd.Parameters.AddWithValue("@id", usuario.CodUsuario);
                cmd.Parameters.AddWithValue("@role", usuario.Role);
                cmd.Parameters.AddWithValue("@nome", usuario.Nome);
                cmd.Parameters.AddWithValue("@email", usuario.Email);
                cmd.Parameters.AddWithValue("@senha", senhaParaSalvar);
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
        // DESATIVAR USUÁRIO
        // ==========================================================
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
                    Nome = reader["Nome"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    Role = reader["Role"]?.ToString() ?? string.Empty,
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
                using var cmd = new MySqlCommand("CALL sp_usuario_atualizar_status(@p_id, @p_status);", conn);
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
                using var cmd = new MySqlCommand("CALL sp_usuario_atualizar_status(@p_id, @p_status);", conn);
                cmd.Parameters.AddWithValue("@p_id", id);
                cmd.Parameters.AddWithValue("@p_status", "S");
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário reativado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao reativar: " + ex.Message;
            }

            return RedirectToAction(nameof(Inativos));
        }

        // ==========================================================
        // Helper: obter senha atual
        // ==========================================================
        private string GetSenhaAtual(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("SELECT Senha FROM Usuario WHERE codUsuario=@id;", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}