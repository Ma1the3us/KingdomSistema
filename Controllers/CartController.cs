using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using System.Text.Json;
using MeuProjetoMVC.Autenticacao;


namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize] 
    public class CartController : Controller
    {
        private readonly string _connectionString;

        public CartController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ==================== Métodos privados ====================
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson != null
                ? JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>()
                : new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        // ==================== Ações do Carrinho ====================

        // Adicionar item
        [HttpPost]
        public IActionResult Add(int id)
        {
            var cart = GetCart();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // ✅ Ajustado para os campos corretos da tabela Produto
            using var cmd = new MySqlCommand(
                "SELECT nomeProduto, Valor, Descricao FROM Produto WHERE codProd=@id",
                conn
            );
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var item = cart.FirstOrDefault(c => c.ProdutoId == id);
                if (item == null)
                {
                    cart.Add(new CartItem
                    {
                        ProdutoId = id,
                        Nome = reader["nomeProduto"] as string ?? string.Empty,
                        Descricao = reader["Descricao"] as string ?? string.Empty,
                        Preco = reader.GetDecimal("Valor"),
                        Quantidade = 1
                    });
                }
                else
                {
                    item.Quantidade++;
                }
            }

            SaveCart(cart);
            return Json(new { success = true, message = "Item adicionado ao carrinho!" });
        }

        // Exibir carrinho
        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.TotalCompra = cart.Sum(c => c.Total);
            return View(cart);
        }

        // Atualizar quantidade
        [HttpPost]
        public IActionResult UpdateQuantity(int produtoId, int quantidade)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProdutoId == produtoId);
            if (item != null && quantidade > 0)
            {
                item.Quantidade = quantidade;
            }
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // Remover item
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProdutoId == id);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // ==================== Checkout ====================

        // Formulário de checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Mensagem"] = "Seu carrinho está vazio!";
                return RedirectToAction("Index");
            }

            ViewBag.TotalCompra = cart.Sum(c => c.Total);
            return View();
        }

        // Finalizar compra
        [HttpPost]
        public IActionResult Checkout(string nomeCliente, string emailCliente, string formaPagamento)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Mensagem"] = "Seu carrinho está vazio!";
                return RedirectToAction("Index");
            }

            decimal total = cart.Sum(c => c.Total);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var tran = conn.BeginTransaction();
                try
                {
                    // 1️⃣ Inserir Pedido
                    using var cmdPedido = new MySqlCommand(
                        @"INSERT INTO Pedidos 
                          (NomeCliente, EmailCliente, Total, FormaPagamento) 
                          VALUES (@nome, @email, @total, @formaPagamento);
                          SELECT LAST_INSERT_ID();",
                        conn, tran);

                    cmdPedido.Parameters.AddWithValue("@nome", nomeCliente);
                    cmdPedido.Parameters.AddWithValue("@email", emailCliente);
                    cmdPedido.Parameters.AddWithValue("@total", total);
                    cmdPedido.Parameters.AddWithValue("@formaPagamento", formaPagamento);

                    int pedidoId = Convert.ToInt32(cmdPedido.ExecuteScalar());

                    // 2️⃣ Inserir Itens do Pedido
                    foreach (var item in cart)
                    {
                        using var cmdItem = new MySqlCommand(
                            @"INSERT INTO ItensPedido 
                              (PedidoId, ProdutoId, Quantidade, ValorUnitario)
                              VALUES (@pedidoId, @produtoId, @quantidade, @ValorUnitario)",
                            conn, tran);

                        cmdItem.Parameters.AddWithValue("@pedidoId", pedidoId);
                        cmdItem.Parameters.AddWithValue("@produtoId", item.ProdutoId);
                        cmdItem.Parameters.AddWithValue("@quantidade", item.Quantidade);
                        cmdItem.Parameters.AddWithValue("@preco", item.Preco);
                        cmdItem.Parameters.AddWithValue("@ValorUnitario", item.ValorUnitario);

                        cmdItem.ExecuteNonQuery();
                    }

                    tran.Commit();

                    // ✅ Limpar carrinho após sucesso
                    SaveCart(new List<CartItem>());
                    TempData["Mensagem"] = $"Compra realizada com sucesso! Pedido nº {pedidoId}";
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["Mensagem"] = "Erro ao finalizar compra: " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["Mensagem"] = "Erro de conexão ou execução: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}