using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using System;
using System.Collections.Generic;

namespace MeuProjetoMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }


        public IActionResult Index()
        {

            var produtos = ObterProdutos();


            if (produtos.Count == 0)
            {
                ViewBag.Mensagem = "⚠️ Nenhum produto encontrado ou erro ao carregar produtos.";
            }

            return View(produtos);
        }


        public IActionResult GetProdutosJson()
        {

            var produtos = ObterProdutos();
            return Json(produtos);
        }


        private List<Produto> ObterProdutos()
        {
            var lista = new List<Produto>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();


                const string sql = @"
            SELECT 
                codProd,
                nomeProduto,
                Valor,
                Descricao,
                Quantidade,
                Imagens        -- <--- ADICIONE ESTA COLUNA
            FROM Produto
            ORDER BY codProd DESC";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Produto
                    {
                        codProd = reader["codProd"] != DBNull.Value ? Convert.ToInt32(reader["codProd"]) : 0,
                        nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty,
                        Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                        Valor = reader["Valor"] != DBNull.Value ? Convert.ToDouble(reader["Valor"]) : 0,
                        Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0,

                        // ✅ Lendo os bytes da imagem (ou null)
                        Imagens = reader["Imagens"] != DBNull.Value
                                    ? (byte[])reader["Imagens"]
                                    : null
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erro ao obter produtos: " + ex.Message);
            }

            return lista;
        }

    }
}
