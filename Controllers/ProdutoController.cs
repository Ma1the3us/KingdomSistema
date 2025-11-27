using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Globalization;
using System.Data;

namespace MeuProjetoMVC.Controllers
{

   
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
                cmd.Parameters.AddWithValue("p_imagens", (object?)produto.Imagens ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_valor", produto.Valor);
                cmd.Parameters.AddWithValue("p_descricao", produto.Descricao ?? string.Empty);
                cmd.Parameters.AddWithValue("p_nomeproduto", produto.nomeProduto ?? string.Empty);
                cmd.Parameters.AddWithValue("p_categorias", produto.codCat > 0 ? produto.codCat : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_codfornecedor", produto.codF > 0 ? produto.codF : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_desconto", produto.Desconto);

                var paramcodProd = new MySqlParameter("p_codProd", MySqlDbType.Int32);
                paramcodProd.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(paramcodProd);

                int linhasAfetadas = cmd.ExecuteNonQuery();


                int novocod = Convert.ToInt32(paramcodProd.Value);


                TempData["Mensagem"] = linhasAfetadas > 0
                    ? "✅ Produto cadastrado com sucesso!"
                    : "⚠️ Não foi possível cadastrar o produto.";

                return RedirectToAction("associarSub", new {codProd = novocod, codCat = produto.codCat});
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "❌ Erro ao cadastrar produto: " + ex.Message;
                RecarregarListas(); return View(produto);
            }
        }

        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
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



        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
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
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0,
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
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult Edit(Produto produto, IFormFile? midia)
        {
            if (!ModelState.IsValid)
            {
                RecarregarListas(); 
                return View(produto);
            }

            Produto? imagems = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            try
            {
                if (midia != null && midia.Length > 0)
                {
                    using var ms = new MemoryStream();
                    midia.CopyTo(ms);
                    produto.Imagens = ms.ToArray();
                }
                else
                {
   
                    using var cmd2 = new MySqlCommand(
                        "SELECT Imagens, quantidadeTotal FROM Produto WHERE codProd = @cod;", conn);

                    cmd2.Parameters.AddWithValue("@cod", produto.codProd);

                    using var rd2 = cmd2.ExecuteReader();

                    byte[] imagemAntiga = Array.Empty<byte>();
                    int quantidadeTotalAntiga = 0;

                    if (rd2.Read())
                    {
                        imagemAntiga = rd2["Imagens"] != DBNull.Value ? (byte[])rd2["Imagens"] : Array.Empty<byte>();
                        quantidadeTotalAntiga = rd2["quantidadeTotal"] != DBNull.Value ? Convert.ToInt32(rd2["quantidadeTotal"]) : 0;
                    }

                    rd2.Close();
                 
                    produto.Imagens = imagemAntiga;

                    // ajustar quantidadeTotal corretamente
                    if (produto.Quantidade <= quantidadeTotalAntiga)
                        produto.quantidadeTotal = quantidadeTotalAntiga;
                    else
                        produto.quantidadeTotal = produto.Quantidade;
                }


           
   

                using var cmd = new MySqlCommand(
                    "editar_produto", conn)
                { CommandType = System.Data.CommandType.StoredProcedure};

                produto.Valor = ConverterDecimal(produto.Valor?.ToString());
                produto.Desconto = ConverterDecimal(produto.Desconto?.ToString());


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
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
        public IActionResult List(string? nomeProduto)
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
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty
                });
            }

            return View(produtos);
        }


        public IActionResult BuscarProdutos(string? nomeProduto)
        {
            var produtos = new List<Produto>();
            MySqlCommand cmd;
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            if(nomeProduto == null)
            {
                cmd = new MySqlCommand("SELECT codProd, Quantidade, Valor, Descricao, nomeProduto FROM Produto;", conn);
            }
            else { 
                cmd = new MySqlCommand(@"
                   select codProd, Quantidade, Valor, Descricao, nomeProduto from Produto where 
                   (@nome = '' or nomeProduto like CONCAT('%',@nome,'%'));  
                 ", conn);
                cmd.Parameters.AddWithValue("@nome", nomeProduto);
            }
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                produtos.Add(new Produto
                {
                    codProd = reader["codProd"] != DBNull.Value ? Convert.ToInt32(reader["codProd"]) : 0,
                    Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0,
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    nomeProduto = reader["nomeProduto"]?.ToString() ?? string.Empty
                });
            }

            return Json(new { sucesso = true, produtos});
        }



        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
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
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
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
                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0,
                    Quantidade = reader["Quantidade"] != DBNull.Value ? Convert.ToInt32(reader["Quantidade"]) : 0
                };
            }

            if (produto == null) return NotFound();
            return View(produto);
        }

        [HttpPost, ActionName("Excluir")]
        [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
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


        public IActionResult Detalhes(int? codProd) {

            if(codProd == null || codProd == 0)
            {
                return BadRequest();
            }

            Produto produtos = new Produto();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(@"
                Select codProd, nomeProduto, Descricao, Quantidade, quantidadeTotal, Valor, Imagens, codCat, codF, Desconto
                From Produto 
                where codProd = @cod
                ", conn);
            cmd.Parameters.AddWithValue("@cod", codProd);

            var rd = cmd.ExecuteReader();
            
            while(rd.Read())
            {
                produtos = new Produto
                {
                    codProd = rd["codProd"] != DBNull.Value ? Convert.ToInt32(rd["codProd"]) : 0,
                    codCat = rd["codCat"] != DBNull.Value ? Convert.ToInt32(rd["codCat"]) : 0,
                    codF = rd["codF"] != DBNull.Value ? Convert.ToInt32(rd["codF"]) : 0,
                    nomeProduto = rd["nomeProduto"]?.ToString() ?? string.Empty,
                    Descricao = rd["Descricao"]?.ToString() ?? string.Empty,
                    Quantidade = rd["Quantidade"] != DBNull.Value ? Convert.ToInt32(rd["Quantidade"]) : 0,
                    Desconto = rd["Desconto"] != DBNull.Value ? Convert.ToDecimal(rd["Desconto"]) : 0,
                    Valor = rd["Valor"] != DBNull.Value ? Convert.ToDecimal(rd["Valor"]) : 0,
                    Imagens = rd["Imagens"] != DBNull.Value? (byte[])rd["Imagens"] : Array.Empty<byte>(),
                    quantidadeTotal = rd["quantidadeTotal"] != DBNull.Value ? Convert.ToInt32(rd["quantidadeTotal"]) : 0
                    
                };
            }

            DetalhesM(codProd);


            return View(produtos);
        }

        // Função que vai estar passando todas as viewbags relacionadas ao detalhe do produto.
        private void DetalhesM(int? codProd)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

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
                            codSub = reader.GetInt32("codSub"),
                            codProd = reader.GetInt32("codProd")
                        });
                    }
                }
            }
            
            ViewBag.ItemSub = itemsub;

            List<ProdutoMidia> midias = new List<ProdutoMidia>();
            using (var cmd = new MySqlCommand(@"
                SELECT codMidia, codProd, tipoMidia, midia, Ordem
                FROM ProdutoMidia
                WHERE codProd = @cod
                ORDER BY Ordem ASC;
                ", conn))
            {
                cmd.Parameters.AddWithValue("@cod", codProd);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        midias.Add(new ProdutoMidia
                        {
                            codMidia = reader.GetInt32("codMidia"),
                            codProd = reader.GetInt32("codProd"),
                            tipoMidia = reader.GetString("tipoMidia"),
                            midia = (byte[]?)reader["midia"],
                            Ordem = reader["Ordem"] != DBNull.Value ? Convert.ToInt32(reader["Ordem"]) : 0
                        });
                    }
                }
            }

            ViewBag.Midias = midias;

            List<wishlist> favoritos = new List<wishlist>();
            using (var cmd = new MySqlCommand(@"select codProd, codUsuario from wishlist where codProd = @codP and codUsuario = @codU",conn))
            {
                cmd.Parameters.AddWithValue("@codP", codProd);
                cmd.Parameters.AddWithValue("@codU", user);
                var rd = cmd.ExecuteReader();

                while(rd.Read())
                {
                    favoritos.Add(new wishlist
                    {
                        codProd = rd.GetInt32("codProd"),
                        codUsuario= rd.GetInt32("codUsuario")
                    });
                    
                        
                }
            }

            ViewBag.Favoritos = favoritos;

        }

        [Route("ProdutoMidia/Exibir/{codMidia}")]
        public IActionResult ExibirMidia(int codMidia)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            ProdutoMidia midia = null;
            using (var cmd = new MySqlCommand("SELECT codMidia, tipoMidia, midia FROM ProdutoMidia WHERE codMidia = @cod", conn))
            {
                cmd.Parameters.AddWithValue("@cod", codMidia);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    midia = new ProdutoMidia
                    {
                        codMidia = reader.GetInt32("codMidia"),
                        tipoMidia = reader.GetString("tipoMidia"),
                        midia = (byte[])reader["midia"]
                    };
                }
            }

            if (midia == null || midia.midia == null)
                return NotFound();

            string contentType = midia.tipoMidia == "Video" ? "video/mp4" : "image/jpeg";
            return File(midia.midia, contentType);
        }



        private decimal ConverterDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return 0;

            return decimal.Parse(
                valor.Replace(",", "."),
                System.Globalization.CultureInfo.InvariantCulture
            );
        }


        public IActionResult AssociarSub(int codProd, int codCat)
        {
            Produto produto = new Produto();
            List<Sub_Categoria> sub = new List<Sub_Categoria>();
            List<Categoria> categoria = new List<Categoria>();
            List<int> selecionadas = new List<int>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // 1️⃣ Buscar produto + categoria + subcategorias da categoria
            using (var cmd = new MySqlCommand(@"
            SELECT 
            p.nomeProduto, p.codProd, p.codCat,
            c.nomeCategoria,
            s.codSub, s.nomeSubcategoria
            FROM Produto p
            INNER JOIN Categorias c ON p.codCat = c.codCat
            INNER JOIN Sub_Categoria s ON c.codCat = s.codCat
            WHERE p.codProd = @codProd AND p.codCat = @codCat;
            ", conn))
            {
                cmd.Parameters.AddWithValue("@codProd", codProd);
                cmd.Parameters.AddWithValue("@codCat", codCat);

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    produto = new Produto
                    {
                        codProd = rd.GetInt32("codProd"),
                        nomeProduto = rd.GetString("nomeProduto")
                    };

                    sub.Add(new Sub_Categoria
                    {
                        codSub = rd.GetInt32("codSub"),
                        nomeSubcategoria = rd.GetString("nomeSubcategoria"),
                        codCat = rd.GetInt32("codCat")
                    });

                    categoria.Add(new Categoria
                    {
                        CodCat = rd.GetInt32("codCat"),
                        NomeCategoria = rd.GetString("nomeCategoria")
                    });
                }

                rd.Close();
            }

            // 2️⃣ Buscar subcategorias JÁ VINCULADAS ao produto
            using (var cmd = new MySqlCommand(@"
            SELECT codSub
            FROM Item_Subcategoria
            WHERE codProd = @codProd;
             ", conn))
            {
                cmd.Parameters.AddWithValue("@codProd", codProd);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    selecionadas.Add(rd.GetInt32("codSub"));
                }

                rd.Close();
            }

            // 3️⃣ Enviar para a View
     
            ViewBag.Categoria = categoria;
            ViewBag.Sub = sub;
            ViewBag.Selecionadas = selecionadas;

            return View(produto);
        }


    }


}
