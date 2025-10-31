using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MeuProjetoMVC.Models;
using System.Text.Json;
using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Services;
using Org.BouncyCastle.Asn1.Ocsp;


namespace MeuProjetoMVC.Controllers
{
    [SessionAuthorize] 
    public class CartController : Controller
    {
        private readonly string _connectionString;
        private readonly IFreteServices _freteService;


        public CartController(IFreteServices freteServices)
        {
            _freteService = freteServices;
        }

        private readonly IEnderecoService _enderecoService;


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
        public IActionResult Add(int id, int quantidade)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            if(user == null)
            {
                TempData["MensagemC"] = "Código de usuário não encontrado";
                return View();
            }

            if(quantidade <= 0)
            {
                TempData["MensagemC"] = "Quantidade inválida";
                return View();
            }


            try
            {
                using var cmd = new MySqlCommand("cad_carrinho", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_cod", id);
                cmd.Parameters.AddWithValue("ca_quantidade", quantidade);
                cmd.Parameters.AddWithValue("c_codUsuario", user);
                cmd.ExecuteNonQuery();

                TempData["MensagemC"] = "Item adicionado ao carrinho";

            }
            catch (MySqlException ex)
            {
                TempData["MensagemC"] = "❌ Erro ao cadastrar inserir o produto no carrinho: " + ex.Message;              
            }
                    
            return View();
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
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);
            using var conn = new MySqlConnection(_connectionString);

            if(quantidade <= 0)
            {
                TempData["MensagemC"] = "Quantidade inserida inválida";
                return RedirectToAction("Index");
            }

            conn.Open();
            using var cmd = new MySqlCommand("editar_carrinho", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_cod", produtoId);
            cmd.Parameters.AddWithValue("ca_quantidade", quantidade);
            cmd.Parameters.AddWithValue("u_cod", user);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // Remover item
        public IActionResult Remove(int id)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("deletar_prod_carrinho", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_cod", id);
            cmd.Parameters.AddWithValue("u_cod", user);
            cmd.ExecuteNonQuery();
            
            return RedirectToAction("Index");
        }

        // ==================== Checkout ====================

        // Formulário de checkout
        

        // Alteração, realizar uma função ou trocar o checkout, para só inserir a forma de pagamento
        // Após a compra, colocar para ele realizar a inserção na haba de entregas o destinatário e seu email
        // Então, criar uma função ou método de formaPag, finalizar compra, que já vai estar redirecionando a página de entrega.
        // E de lá... finalizar todo o processo.


        public void formaPagamento(string? forma)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);
            using var conn = new MySqlConnection(_connectionString);

            using var cmd = new MySqlCommand("inserir_formaPagamento", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("u_formaPag", forma);
            cmd.Parameters.AddWithValue("u_codUsuario", user);
            cmd.ExecuteNonQuery();
        }

        //TESTE TOTALMENTE ALEATÓRIO DE API! EU REALMENTE PRECISO VER COMO ISSO FUNCIONA!!!!!!!!
        public async  Task<IActionResult> concluirCompra(string? Cupon, string? cep)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            List<EnderecoEntrega> entrega = new List<EnderecoEntrega>();

            using var conn = new MySqlConnection(_connectionString);

            string cepOrigem = "01001-000"; // CEP da origem fixo (exemplo)

            if(cep == null)
            {
                return BadRequest();
            }

            double frete = await _freteService.CalcularFreteAsync(cepOrigem, cep);

            var endereco = await _enderecoService.ObterEnderecoPorCepAsync(cep);

            if(endereco == null)
            {
                return BadRequest();
            }

            entrega.Add(new EnderecoEntrega
            {
                Bairro = endereco.Bairro,
                Logradouro = endereco.Logradouro,
                Cidade = endereco.Localidade,
                Estado = endereco.Uf,
                Cep = cep
            });
        
            using var cmd = new MySqlCommand("concluir_compra") { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("u_cod", user);
            cmd.Parameters.AddWithValue("v_codigoCupom", Cupon);
            cmd.Parameters.AddWithValue("v_frete", frete);

            cmd.ExecuteNonQuery();

            ViewBag.Endereço = entrega;

            //Return RedirectToAction("","");
            return View();
        }
            


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
                        cmdItem.Parameters.AddWithValue("@produtoId", item.codProd);
                        cmdItem.Parameters.AddWithValue("@quantidade", item.Quantidade);
                        cmdItem.Parameters.AddWithValue("@preco", item.ValorUnitario);
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