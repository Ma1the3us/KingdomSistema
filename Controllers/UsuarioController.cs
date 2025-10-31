using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using System;
using System.Collections.Generic;
using BCrypt.Net;
using static System.Net.Mime.MediaTypeNames;

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
                        Ativo = reader["Ativo"]?.ToString() ?? "1"
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
                        Ativo = "0"
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
        public IActionResult Criar(Usuario usuario, IFormFile? capa)
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


                if (capa != null && capa.Length > 0)
                {
                    using var ms = new MemoryStream();
                    capa.CopyTo(ms);
                    usuario.Imagens = ms.ToArray();
                }


                var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha, workFactor: 12);

                using var cmd = new MySqlCommand("cadastrar_usuario", conn) {CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_role", usuario.Role);
                cmd.Parameters.AddWithValue("p_nome", usuario.Nome);
                cmd.Parameters.AddWithValue("p_email", usuario.Email);
                cmd.Parameters.AddWithValue("p_senha", senhaHash);
                cmd.Parameters.AddWithValue("p_foto", usuario.Imagens);
                cmd.Parameters.AddWithValue("p_telefone", usuario.Telefone);


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


        // CADASTRO PRÓPRIO PARA O CLIENTE MESMO FAZER, TENDO A ROLE JÁ DEFINIDA COMO CLIENTE!!!!!!

       

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
                        Role = reader["Role"]?.ToString() ?? "Role",
                        Nome = reader["Nome"]?.ToString() ?? "Nome",
                        Email = reader["Email"]?.ToString() ?? "Email",
                        Ativo = reader["Ativo"]?.ToString() ?? "1",
                        Senha = "",
                        ConfirmarSenha = "",
                        Imagens = reader["Foto"] != DBNull.Value ? (byte[])reader["Foto"] : null,
                        Telefone = reader["Telefone"]?.ToString()
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

                string? ativo = "";
                
                if (usuario.Ativo == "Ativado")
                 {
                    ativo = "1";
                }
                else
                {
                    ativo = "0";
                }

                using var cmd = new MySqlCommand(
                    "atualizar_usuario_adm", conn)
                { CommandType = System.Data.CommandType.StoredProcedure};
                cmd.Parameters.AddWithValue("p_codUsuario", usuario.CodUsuario);
                cmd.Parameters.AddWithValue("p_nome", usuario.Nome);
                cmd.Parameters.AddWithValue("p_email", usuario.Email);
                cmd.Parameters.AddWithValue("p_role", usuario.Role);
                cmd.Parameters.AddWithValue("p_senha", senhaFinal);
                cmd.Parameters.AddWithValue("p_telefone", usuario.Telefone);
                cmd.Parameters.AddWithValue("p_foto", usuario.Imagens);
                cmd.Parameters.AddWithValue("p_ativo", ativo);

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



        // ------------------------------------------------------------------------------
        // PARA O PRÓPRIO CLIENTE SE EDITAR SEM O ROLE! OU FUNCIONARIO EDITAR O CLIENTE -
        // ------------------------------------------------------------------------------

        public IActionResult EditarCliente(int id)
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
                        Role = reader["Role"]?.ToString() ?? "Role",
                        Nome = reader["Nome"]?.ToString() ?? "Nome",
                        Email = reader["Email"]?.ToString() ?? "Email",
                        Ativo = reader["Ativo"]?.ToString() ?? "1",
                        Senha = "",
                        ConfirmarSenha = "",
                        Imagens = reader["Foto"] != DBNull.Value ? (byte[])reader["Foto"] : null,
                        Telefone = reader["Telefone"]?.ToString()
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
        public IActionResult EditarCliente(Usuario usuario)
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

                string? ativo = "";

                if (usuario.Ativo == "Ativado")
                {
                    ativo = "1";
                }
                else
                {
                    ativo = "0";
                }

                using var cmd = new MySqlCommand(
                    "alterar_usuario", conn)
                { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_codUsuario", usuario.CodUsuario);
                cmd.Parameters.AddWithValue("p_nome", usuario.Nome);
                cmd.Parameters.AddWithValue("p_email", usuario.Email);
                cmd.Parameters.AddWithValue("p_senha", senhaFinal);
                cmd.Parameters.AddWithValue("p_telefone", usuario.Telefone);
                cmd.Parameters.AddWithValue("p_foto", usuario.Imagens);
                
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
                    Ativo = reader["Ativo"]?.ToString() ?? "1",
                    Imagens = reader["Foto"] != DBNull.Value ? (byte[])reader["Foto"] : null,
                    Telefone = reader["Telefone"]?.ToString()
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
                    "desativar_usuario", conn)
                { CommandType = System.Data.CommandType.StoredProcedure};
                cmd.Parameters.AddWithValue("p_codUsuario", id);
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
                cmd.Parameters.AddWithValue("@p_status", "1");
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário reativado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao reativar usuário: " + ex.Message;
            }

            return RedirectToAction(nameof(Inativos));
        }


        // Quando o cliente tentar logar, mas se o ativo estiver 0, podemos redefinir para essa página
        // Onde ele vai ativar o usuário dele, ou só realizar o método, pode ser uma view ou só um método executavel
        [HttpGet]
        public IActionResult ReativarCliente(string? email)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(
                    "ativar_cliente", conn)
                { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("c_email", email);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Usuário reativado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao reativar usuário: " + ex.Message;
            }

            return RedirectToAction("Auth","Login");
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
