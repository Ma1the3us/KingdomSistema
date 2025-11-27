using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using System;
using System.Collections.Generic;

namespace MeuProjetoMVC.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly string _connectionString;

        public CategoriasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ==========================================================
        // LISTAR CATEGORIAS
        // ==========================================================
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Index()
        {
            var categorias = new List<Categoria>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("CALL sp_listar_categorias();", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                categorias.Add(new Categoria
                {
                    CodCat = reader.GetInt32("codCat"),
                    NomeCategoria = reader.GetString("nomeCategoria")
                });
            }

            return View(categorias);
        }

        public IActionResult BuscarCategoria(string? nome)
        {
            var categorias = new List<Categoria>();

            var conn = new MySqlConnection(_connectionString);
            conn.Open();

            MySqlCommand cmd;

            if (nome == null)
            {
                cmd = new MySqlCommand(@"
                SELECT codCat, nomeCategoria
                FROM Categorias
                order by nomeCategoria;
                ", conn);
            }
            else
            {
                cmd = new MySqlCommand(@"
                SELECT codCat, nomeCategoria
                FROM Categorias
                where
                (@nome = '' or nomeCategoria like concat('%',@nome,'%'));
                ", conn);
                cmd.Parameters.AddWithValue("@nome", nome);
            }
           
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                categorias.Add(new Categoria
                {
                    CodCat = reader.GetInt32("codCat"),
                    NomeCategoria = reader.GetString("nomeCategoria")
                });
            }

            return Json(new
            {
                sucesso = true,
                categorias

            });
        }



        // ==========================================================
        // CRIAR CATEGORIA
        // ==========================================================
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Criar() => View(new Categoria());

        [HttpPost]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Criar(Categoria categoria)
        {
            if (!ModelState.IsValid) return View(categoria);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("CALL cad_categoria(@p_nome);", conn);
                cmd.Parameters.AddWithValue("@p_nome", categoria.NomeCategoria);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Categoria cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao cadastrar: " + ex.Message;
                return View(categoria);
            }
        }

        // ==========================================================
        // EDITAR CATEGORIA (GET)
        // ==========================================================
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Editar(int id)
        {
            Categoria? categoria = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("CALL sp_obter_categoria(@p_id);", conn);
            cmd.Parameters.AddWithValue("@p_id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                categoria = new Categoria
                {
                    CodCat = reader.GetInt32("codCat"),
                    NomeCategoria = reader.GetString("nomeCategoria")
                };
            }

            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // ==========================================================
        // EDITAR CATEGORIA (POST)
        // ==========================================================
        [HttpPost]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Editar(Categoria categoria)
        {
            if (!ModelState.IsValid) return View(categoria);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("CALL sp_atualizar_categoria(@p_id, @p_nome);", conn);
                cmd.Parameters.AddWithValue("@p_id", categoria.CodCat);
                cmd.Parameters.AddWithValue("@p_nome", categoria.NomeCategoria);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Categoria atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao editar: " + ex.Message;
                return View(categoria);
            }
        }

        // ==========================================================
        // EXCLUIR CATEGORIA (GET)
        // ==========================================================
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Excluir(int id)
        {
            Categoria? categoria = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("CALL sp_obter_categoria(@p_id);", conn);
            cmd.Parameters.AddWithValue("@p_id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                categoria = new Categoria
                {
                    CodCat = reader.GetInt32("codCat"),
                    NomeCategoria = reader.GetString("nomeCategoria")
                };
            }

            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // ==========================================================
        // EXCLUIR CATEGORIA (POST)
        // ==========================================================
        [HttpPost, ActionName("Excluir")]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("CALL sp_excluir_categoria(@p_id);", conn);
                cmd.Parameters.AddWithValue("@p_id", id);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Categoria excluída com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao excluir: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
