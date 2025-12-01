using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Autenticacao;
using System.Text.Json;
using System.Data;

namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize(RoleAnyOf = "Admin,Funcionario,Cliente")]
    public class CartController : Controller
    {
        private readonly string _connectionString;

        public CartController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ====================
        // ADICIONAR ITEM
        // ====================
        [HttpPost]
        public async Task<IActionResult> Add(int codProd, int quantidade)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

           

            if (user == null)
            {
                TempData["MensagemC"] = "Usuário não encontrado";
                return RedirectToAction("Login", "Auth");
            }

            if (quantidade <= 0)
            {
                TempData["MensagemC"] = "Quantidade inválida";
                return RedirectToAction("Index", "Home");
            }

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("cad_carrinho", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("p_cod", codProd);
            cmd.Parameters.AddWithValue("ca_quantidade", quantidade);
            cmd.Parameters.AddWithValue("c_codUsuario", user);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                TempData["MensagemC"] = "Item adicionado ao carrinho!";
            }
            catch (Exception ex)
            {
                TempData["MensagemC"] = "Erro ao adicioner: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ====================
        // EXIBIR CARRINHO
        // ====================
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (user == null)
            {
                TempData["ErroL"] = "Faça login para acessar o carrinho.";
                return RedirectToAction("Login", "Auth");
            }

            var itens = new List<CartItem>();
            var produtos = new List<Produto>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT 
                    ic.codProd, ic.quantidade, ic.valorProduto,
                    ic.codCarrinho, p.nomeProduto,
                    p.Imagens, p.Valor, p.Desconto
                FROM ItemCarrinho ic
                JOIN Carrinho c ON ic.codCarrinho = c.codCarrinho
                JOIN Venda v ON c.codVenda = v.codVenda
                JOIN Produto p ON ic.codProd = p.codProd
                WHERE v.codUsuario = @u AND v.situacao = 'Em andamento';
            ", conn);

            cmd.Parameters.AddWithValue("@u", user);

            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                itens.Add(new CartItem
                {
                    codCarrinho = rd.GetInt32("codCarrinho"),
                    codProd = rd.GetInt32("codProd"),
                    Quantidade = rd.GetInt32("quantidade"),
                    Valor = rd.GetDecimal("valorProduto")
                });

                produtos.Add(new Produto
                {
                    codProd = rd.GetInt32("codProd"),
                    nomeProduto = rd.GetString("nomeProduto"),
                    Valor = rd.GetDecimal("Valor"),
                    Imagens = rd["Imagens"] is DBNull ? Array.Empty<byte>() : (byte[])rd["Imagens"],
                    Desconto = rd["Desconto"] != DBNull.Value ? rd.GetDecimal("Desconto") : 0
                });
            }

            ViewBag.TotalCompra = itens.Sum(i => (double)i.Valor);
            ViewBag.Produtos = produtos;

            return View(itens);
        }

        // ====================
        // ALTERAR QUANTIDADE
        // ====================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int produtoId, int quantidade)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (quantidade <= 0)
            {
                TempData["MensagemC"] = "Quantidade inválida";
                return RedirectToAction("Index");
            }

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("editar_carrinho", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("p_cod", produtoId);
            cmd.Parameters.AddWithValue("ca_quantidade", quantidade);
            cmd.Parameters.AddWithValue("u_cod", user);

            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        // ====================
        // REMOVER ITEM
        // ====================
        public async Task<IActionResult> Remove(int id)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("deletar_prod_carrinho", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("p_cod", id);
            cmd.Parameters.AddWithValue("u_cod", user);

            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }
    }
}
