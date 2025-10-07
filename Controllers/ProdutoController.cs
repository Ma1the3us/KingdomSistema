using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
    public class ProdutoController : Controller
    {
        private readonly string _connectionString;

        public ProdutoController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string não encontrada");
        }




        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Create()
        {
            RecarregarListas();
            return View(new Produto());
        }

        [HttpPost]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Create(Produto produto, IFormFile ImagemUpload)
        {
            if (!ModelState.IsValid)
            {
                RecarregarListas(); return View(produto);
            }

            try
            {
                if (ImagemUpload != null && ImagemUpload.Length > 0)
                {
                    using var ms = new MemoryStream();
                    ImagemUpload.CopyTo(ms);
                    produto.Imagens = ms.ToArray();
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand(
            @"CALL cad_Produto(@quantidade,
                       @imagens,
                       @valor,
                       @descricao,
                       @nomeProduto,
                       @codCat,
                       @codF);", conn);

                cmd.Parameters.AddWithValue("@quantidade", produto.Quantidade);
                cmd.Parameters.AddWithValue("@imagens", (object?)produto.Imagens ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@valor", produto.Valor);
                cmd.Parameters.AddWithValue("@descricao", produto.Descricao ?? string.Empty);
                cmd.Parameters.AddWithValue("@nomeProduto", produto.nomeProduto ?? string.Empty);
                cmd.Parameters.AddWithValue("@codCat", produto.codCat > 0 ? produto.codCat : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@codF", produto.codF > 0 ? produto.codF : (object)DBNull.Value);

                int linhasAfetadas = cmd.ExecuteNonQuery();

                TempData["Mensagem"] = linhasAfetadas > 0
                    ? "✅ Produto cadastrado com sucesso!"
                    : "⚠️ Não foi possível cadastrar o produto.";

                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "❌ Erro ao cadastrar produto: " + ex.Message;
                RecarregarListas(); return View(produto);
            }
        }

        private void RecarregarListas()
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();


            var categorias = new List<Categoria>();
            using (var cmd = new MySqlCommand("SELECT codCat, nomeCategoria FROM Categorias ORDER BY nomeCategoria;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    categorias.Add(new Categoria
                    {
                        CodCat = reader.GetInt32("codCat"),
                        NomeCategoria = reader.GetString("nomeCategoria")
                    });
                }
            }

            ViewBag.Categorias = new SelectList(categorias, "CodCat", "NomeCategoria");

            var fornecedores = new List<Fornecedor>();
            using (var cmd = new MySqlCommand("SELECT codF, Nome FROM Fornecedor ORDER BY Nome;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    fornecedores.Add(new Fornecedor
                    {
                        CodF = reader.GetInt32("codF"),
                        Nome = reader.GetString("Nome")
                    });
                }
            }
            ViewBag.Categorias = new SelectList(categorias, "CodCat", "NomeCategoria");
            ViewBag.Fornecedores = new SelectList(fornecedores, "CodF", "Nome");
        }




        public IActionResult Edit(int id)
        {
            Produto? produto = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT codProd, Quantidade,  Valor, Descricao, nomeProduto FROM Produto WHERE codProd=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                produto = new Produto
                {
                    codProd = reader["codProd"] != DBNull.Value ? Convert.ToInt32(reader["codProd"]) : 0,
                    Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0,
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDouble(reader["Valor"]) : 0,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty
                };
            }

            if (produto == null) return NotFound();
            return View(produto);
        }

        [HttpPost]
        public IActionResult Edit(Produto produto, IFormFile ImagemUpload)
        {
            if (!ModelState.IsValid)
            {
                RecarregarListas(); return View(produto);
            }

            try
            {
                if (ImagemUpload != null && ImagemUpload.Length > 0)
                {
                    using var ms = new MemoryStream();
                    ImagemUpload.CopyTo(ms);
                    produto.Imagens = ms.ToArray();
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand(
                    @"CALL editar_produto(
                @cod,
                @quant,
                @valor,
                @nome,
                @descricao,
                @imagens
            );", conn);

                cmd.Parameters.AddWithValue("@cod", produto.codProd);
                cmd.Parameters.AddWithValue("@quant", produto.Quantidade);
                cmd.Parameters.AddWithValue("@valor", produto.Valor);
                cmd.Parameters.AddWithValue("@nome", produto.nomeProduto ?? string.Empty);
                cmd.Parameters.AddWithValue("@descricao", produto.Descricao ?? string.Empty);
                cmd.Parameters.AddWithValue("@imagens", (object?)produto.Imagens ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Produto atualizado com sucesso!";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = "Erro ao atualizar produto: " + ex.Message;
                RecarregarListas();
                return View(produto);
            }
        }
        // GET: /Produto/List
        public IActionResult List()
        {
            var produtos = new List<Produto>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT codProd, Quantidade, Valor, Descricao, nomeProduto FROM Produto;", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                produtos.Add(new Produto
                {
                    codProd = reader["codProd"] != DBNull.Value ? Convert.ToInt32(reader["codProd"]) : 0,
                    Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0,
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDouble(reader["Valor"]) : 0,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty
                });
            }

            return View(produtos);
        }
        private void PopularViewBags()
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var categorias = new List<SelectListItem>();
            using (var cmd = new MySqlCommand("SELECT codCat, nomeCategoria FROM Categorias ORDER BY nomeCategoria;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    categorias.Add(new SelectListItem
                    {
                        Value = reader["codCat"].ToString(),
                        Text = reader["nomeCategoria"].ToString()
                    });
                }
            }
            ViewBag.Categorias = categorias;

            var fornecedores = new List<SelectListItem>();
            using (var cmd = new MySqlCommand("SELECT codF, Nome FROM Fornecedor ORDER BY Nome;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    fornecedores.Add(new SelectListItem
                    {
                        Value = reader["codF"].ToString(),
                        Text = reader["Nome"].ToString()
                    });
                }
            }
            ViewBag.Fornecedores = fornecedores;
        }

        [ActionName("Excluir")]
        public IActionResult Excluir(int id)
        {
            Produto? produto = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT codProd, nomeProduto, Descricao, Valor, Quantidade FROM Produto WHERE codProd=@id;", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                produto = new Produto
                {
                    codProd = reader["codProd"] != DBNull.Value ? Convert.ToInt32(reader["codProd"]) : 0,
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDouble(reader["Valor"]) : 0,
                    Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0
                };
            }

            if (produto == null) return NotFound();
            return View(produto);
        }

        [HttpPost, ActionName("Excluir")]
        public IActionResult ExcluirConfirmado(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("DELETE FROM Produto WHERE codProd=@id;", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                TempData["Mensagem"] = "Produto excluído com sucesso!";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro ao excluir produto: " + ex.Message;
                return RedirectToAction("List");
            }
        }


    }


}
