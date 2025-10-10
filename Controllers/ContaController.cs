/*using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MeuProjetoMVC.Models;
using MySql.Data.MySqlClient;
using System;
using BCrypt.Net;

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

        [HttpGet]
        public IActionResult Registrar()
        {
            return View(new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(Usuario usuario)
        {
            if (!ModelState.IsValid)
                return View(usuario);

            try
            {
                // Forçar o role como 'Cliente'
                usuario.Role = "Cliente";

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

                TempData["Mensagem"] = "Cadastro realizado com sucesso!";
                return RedirectToAction("Login", "Conta"); // redirecione para tela de login
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao cadastrar: " + ex.Message;
                return View(usuario);
            }
        }
    }
}
*/