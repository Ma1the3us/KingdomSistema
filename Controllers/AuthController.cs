using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using System;

namespace MeuProjetoMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string não encontrada");
        }


        [HttpGet]
        public IActionResult Login()
        {

            if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        //Perguntar se o usuário deve ter a foto trazida diretamente ou não

        [HttpPost]

        public IActionResult Login(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Erro = "Preencha todos os campos.";
                return View();
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();


            using var cmd = new MySqlCommand(
                "SELECT codUsuario, Nome, Email, Senha, Role, Ativo FROM Usuario WHERE Email = @email LIMIT 1;",
                conn);
            cmd.Parameters.AddWithValue("@email", email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                ViewBag.Erro = "Usuário não encontrado ou email inválido.";
                return View();
            }

            var codUsuario = reader.GetInt32("codUsuario");
            var nome = reader.GetString("Nome");
            var emailDb = reader.GetString("Email");
            var senhaHash = reader.GetString("Senha");
            var role = reader.GetString("Role");
            var ativo = reader.GetString("Ativo");

            //Tem que redirecionar para a área de ativição do usuário quando for cliente.
            if (ativo != "1")
            {
                ViewBag.Erro = "Usuário inativo. Contate o administrador.";
                return View();
            }


            bool senhaValida = BCrypt.Net.BCrypt.Verify(senha, senhaHash);
            if (!senhaValida)
            {
                ViewBag.Erro = "Senha inválida.";
                return View();
            }


            HttpContext.Session.SetInt32(SessionKeys.UserId, codUsuario);
            HttpContext.Session.SetString(SessionKeys.UserName, nome);
            HttpContext.Session.SetString(SessionKeys.UserEmail, emailDb);
            HttpContext.Session.SetString(SessionKeys.UserRole, role);


            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult Logout()
        {

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }
    }
}
