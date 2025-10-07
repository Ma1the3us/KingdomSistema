using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using System;
using System.Collections.Generic;

using MeuProjetoMVC.Autenticacao;

namespace MeuProjetoMVC.Controllers
{
    public class FornecedoresController : Controller
    {
        private readonly string _connectionString;

        public FornecedoresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Index()
        {
            var fornecedores = new List<Fornecedor>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT codF, CNPJ, Nome FROM Fornecedor;", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                fornecedores.Add(new Fornecedor
                {
                    CodF = reader.GetInt32("codF"),
                    CNPJ = reader.GetInt64("CNPJ"),
                    Nome = reader.GetString("Nome")
                });
            }

            return View(fornecedores);
        }

        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Criar()
        {
            return View(new Fornecedor());
        }

        [HttpPost]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Criar(Fornecedor fornecedor)
        {
            if (!ModelState.IsValid) return View(fornecedor);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("CALL cad_Fornecedor(@p_CNPJ, @p_Nome);", conn);
                cmd.Parameters.AddWithValue("@p_CNPJ", fornecedor.CNPJ);
                cmd.Parameters.AddWithValue("@p_Nome", fornecedor.Nome);

                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Fornecedor cadastrado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao cadastrar: " + ex.Message;
                return View(fornecedor);
            }

        }

        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Editar(int id)
        {
            Fornecedor? fornecedor = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT codF, CNPJ, Nome FROM Fornecedor WHERE codF=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                fornecedor = new Fornecedor
                {
                    CodF = reader.GetInt32("codF"),
                    CNPJ = reader.GetInt64("CNPJ"),
                    Nome = reader.GetString("Nome")
                };
            }

            if (fornecedor == null) return NotFound();
            return View(fornecedor);
        }

        [HttpPost]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Editar(Fornecedor fornecedor)
        {
            if (!ModelState.IsValid) return View(fornecedor);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(
                    "UPDATE Fornecedor SET CNPJ=@cnpj, Nome=@nome WHERE codF=@id;", conn);
                cmd.Parameters.AddWithValue("@cnpj", fornecedor.CNPJ);
                cmd.Parameters.AddWithValue("@nome", fornecedor.Nome);
                cmd.Parameters.AddWithValue("@id", fornecedor.CodF);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Fornecedor atualizado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao editar: " + ex.Message;
                return View(fornecedor);
            }
        }

        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Excluir(int id)
        {
            Fornecedor? fornecedor = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT codF, CNPJ, Nome FROM Fornecedor WHERE codF=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                fornecedor = new Fornecedor
                {
                    CodF = reader.GetInt32("codF"),
                    CNPJ = reader.GetInt64("CNPJ"),
                    Nome = reader.GetString("Nome")
                };
            }

            if (fornecedor == null) return NotFound();
            return View(fornecedor);
        }

        [HttpPost, ActionName("Excluir")]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult ExcluirConfirmado(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("DELETE FROM Fornecedor WHERE codF=@id;", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Fornecedor excluído com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao excluir: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
