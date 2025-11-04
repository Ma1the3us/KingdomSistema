using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using static System.Net.Mime.MediaTypeNames;

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
        public IActionResult Create(Produto produto, IFormFile midia)
        {
            if (!ModelState.IsValid)
            {
                RecarregarListas(); return View(produto);
            }

            try
            {
                if (midia != null && midia.Length > 0)
                {
                    using var ms = new MemoryStream();
                    midia.CopyTo(ms);
                    produto.Imagens = ms.ToArray();
                }

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand(
                "cad_Produto", conn)
                { CommandType = System.Data.CommandType.StoredProcedure};

                cmd.Parameters.AddWithValue("p_quantidade", produto.Quantidade);
                cmd.Parameters.AddWithValue("p_imagens",(object?) produto.Imagens);
                cmd.Parameters.AddWithValue("p_desconto", produto.Desconto);
                cmd.Parameters.AddWithValue("p_valor", produto.Valor);
                cmd.Parameters.AddWithValue("p_descricao", produto.Descricao ?? string.Empty);
                cmd.Parameters.AddWithValue("p_nomeProduto", produto.nomeProduto ?? string.Empty);
                cmd.Parameters.AddWithValue("p_categorias", produto.codCat > 0 ? produto.codCat : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_codfornecedor", produto.codF > 0 ? produto.codF : (object)DBNull.Value);

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
                "SELECT codProd, Quantidade, quantidadeTotal,Imagens,Valor, Descricao, nomeProduto, codCat, codF, Desconto FROM Produto WHERE codProd=@id;", conn);
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
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty,
                    quantidadeTotal = reader["quantidadeTotal"] != DBNull.Value ? Convert.ToInt32(reader["quantidadeTotal"]) : 0,
                    Imagens =  reader["Imagens"] != DBNull.Value ? (byte[])reader["Imagens"] : Array.Empty<byte>(),
                    Desconto = reader["Desconto"] != DBNull.Value ? Convert.ToInt32(reader["Desconto"]) : 0,
                    codCat = reader["codCat"] != DBNull.Value ? Convert.ToInt32(reader["codCat"]) : 0,
                    codF = reader["codF"] != DBNull.Value ? Convert.ToInt32(reader["codF"]) : 0,
 
                };
            }

            if (produto == null) return NotFound();
            return View(produto);
        }

        [HttpPost]
        public IActionResult Edit(Produto produto, IFormFile midia)
        {
            if (!ModelState.IsValid)
            {
                RecarregarListas(); return View(produto);
            }

            try
            {
                if (midia != null && midia.Length > 0)
                {
                    using var ms = new MemoryStream();
                    midia.CopyTo(ms);
                    produto.Imagens = ms.ToArray();
                }

                if(produto.Quantidade > produto.quantidadeTotal)
                {
                    produto.quantidadeTotal = produto.Quantidade;
                }


                using var conn = new MySqlConnection(_connectionString);
                
                conn.Open();

                using var cmd = new MySqlCommand(
                    "editar_produto", conn)
                { CommandType = System.Data.CommandType.StoredProcedure};


    

                cmd.Parameters.AddWithValue("p_cod", produto.codProd);
                cmd.Parameters.AddWithValue("p_quant", produto.Quantidade);
                cmd.Parameters.AddWithValue("p_quanttotal", produto.quantidadeTotal);
                cmd.Parameters.AddWithValue("p_valor", produto.Valor);
                cmd.Parameters.AddWithValue("p_nome", produto.nomeProduto ?? string.Empty);
                cmd.Parameters.AddWithValue("p_descricao", produto.Descricao ?? string.Empty);
                cmd.Parameters.AddWithValue("p_imagens", (object?)produto.Imagens ?? string.Empty);
                cmd.Parameters.AddWithValue("p_desconto", (double?)produto.Desconto ?? 0);
                cmd.Parameters.AddWithValue("p_cat", produto.codCat);
                cmd.Parameters.AddWithValue("p_for", produto.codF);

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




        // Perguntar se não é melhor só desativar o produto do que excluir ele completamente
        // Já que é provavel que seja cascata.
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

        public IActionResult Detalhes(int codProd) {

            Produto produtos = new Produto();


            return View();
        }

        private void DetalhesM(int codProd)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            List<Categoria> cat = new List<Categoria>();
            using (var cmd = new MySqlCommand("SELECT codCat, nomeCategoria FROM Categorias ORDER BY nomeCategoria;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    cat.Add(new Categoria
                    {
                        CodCat = reader.GetInt32("codCat"),
                        NomeCategoria = reader.GetString("nomeCategoria")
                    });
                }
            }
            ViewBag.Categorias = cat;

            List<Fornecedor> fornecedor = new List<Fornecedor>();
            using (var cmd = new MySqlCommand("SELECT codF, Nome FROM Fornecedor ORDER BY Nome;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    fornecedor.Add(new Fornecedor 
                    {
                       CodF = reader.GetInt32("codF") ,
                       Nome = reader.GetString("Nome")                   
                    });
                }
            }
            ViewBag.Fornecedores = fornecedor;
            
            List<Sub_Categoria> sub = new List<Sub_Categoria>();
            using (var cmd = new MySqlCommand("Select codSub, nomeSubcategoria, codCat from Sub_Categoria", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    sub.Add(new Sub_Categoria
                    {
                        codCat = reader.GetInt32("codCat"),
                        nomeSubcategoria = reader.GetString("nomeSubcategoria"),
                        codSub = reader.GetInt32("codSub")

                    });
                }
            }
            ViewBag.Subcategoria = sub;


            List<ItemSubcategoria> itemsub = new List<ItemSubcategoria>();
            using (var cmd = new MySqlCommand("Select codSub, codProd from Item_Subcategoria where codProd = @cod", conn))
            {
                cmd.Parameters.AddWithValue("@cod", codProd);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        itemsub.Add(new ItemSubcategoria
                        {
                            codSub = reader.GetInt32("codProd"),
                            codProd = reader.GetInt32("codSub")
                        });
                    }
                }
            }
            
            ViewBag.ItemSub = itemsub;

            List<ProdutoMidia> produtoMidias = new List<ProdutoMidia>();
            using (var cmd = new MySqlCommand("Select codMidia, codProd, tipoMidia, midia from ProdutoMidia where codProd = @cod", conn))
            {
                cmd.Parameters.AddWithValue("@cod", codProd);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        produtoMidias.Add(new ProdutoMidia
                        {
                            codMidia = reader.GetInt32("codMidia"),
                            codProd = reader.GetInt32("codProd"),
                            tipoMidia = reader.GetString("tipoMidia"),
                            midia = reader.GetString("midia")

                        });

                    }
                }

            }
            ViewBag.ProdutoMidia = produtoMidias;

        }


    }


}
