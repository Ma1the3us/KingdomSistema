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


        public IActionResult Index(string? busca)
        {

            var produtos = ObterProdutos();
           

            if (produtos.Count == 0)
            {
                ViewBag.Mensagem = "⚠️ Nenhum produto encontrado ou erro ao carregar produtos.";
            }

            if(busca != null)
            {
                produtos = produtos
                .FindAll(p => p.nomeProduto.Contains(busca, StringComparison.OrdinalIgnoreCase));

                if (produtos.Count == 0)
                {
                    ViewBag.Mensagem = "⚠️ Nenhum produto encontrado.";
                }
            }

            ViewBag.produtosCategoria = PesquisarProdutosPorCategorias();

            ViewBag.Busca = busca;

            return View(produtos);
        }

        public List<CategoriaProdutos> PesquisarProdutosPorCategorias(int topPorCategoria = 10)
        {
            var lista = new List<(int codCat, string nomeCat, Produto P)>();

            var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = @"
                Select
                c.codCat, c.nomeCategoria,
                p.codProd, p.nomeProduto, p.Valor, p.Imagens, p.Descricao,p.Quantidade
                from Produto p
                inner Join Categorias c on p.codCat = c.codCat
                where p.quantidade > 0
                order by c.codCat, p.codProd Desc
             ";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var produto = new Produto
                {
                    codProd = reader["codProd"] != DBNull.Value ? Convert.ToInt32(reader["codProd"]) : 0,
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0,
                    Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0,
                    Imagens = reader["Imagens"] != DBNull.Value ? (byte[])reader["Imagens"] : null,
                    codCat = reader["CodCat"] != DBNull.Value ? Convert.ToInt32(reader["CodCat"]) : 0
                };

                var codCat = reader["CodCat"] != DBNull.Value ? Convert.ToInt32(reader["CodCat"]) : 0;
                var nomeCat = reader["NomeCategoria"]?.ToString() ?? "Sem categoria";

                lista.Add((codCat, nomeCat, produto));
            }

            var grouped = lista
                .GroupBy(x => new { x.codCat, x.nomeCat })
                .Select(g => new CategoriaProdutos
                {
                    codCat = g.Key.codCat,
                    nomeCategoria = g.Key.nomeCat,
                    Produtos = g.Select(x => x.P).Take(topPorCategoria).ToList()
                })
                .ToList();

            return grouped;
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
            Where Quantidade > 0
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
                        Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0,
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
