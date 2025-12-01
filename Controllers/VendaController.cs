using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using MeuProjetoMVC.Services;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto.Digests;
using System.Data;
using System.Text.Json;

namespace MeuProjetoMVC.Controllers
{
    [Route("sistema/Venda")]
    public class VendaController : Controller
    {
        private readonly string _connectionString;
        private readonly IFreteServices _freteService;
        private readonly IEnderecoService _enderecoService;

        public VendaController(IConfiguration configuration,
                               IFreteServices freteService,
                               IEnderecoService enderecoService)
        {
            _freteService = freteService;
            _enderecoService = enderecoService;

            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (user == null || user == 0)
                return RedirectToAction("Auth", "Login");

            var conn = new MySqlConnection(_connectionString);
            conn.Open();

            List<Venda> venda = new List<Venda>();
            List<CartItem> carrinho = new List<CartItem>();
            List<Produto> produto = new List<Produto>();
            List<cartaoCli> cartao = new List<cartaoCli>();

      

            using var cmd = new MySqlCommand(@"
               Select
                ic.codProd, ic.codCarrinho,ic.quantidade,v.codVenda, ic.valorProduto, p.nomeProduto
                From ItemCarrinho ic
                Inner Join Carrinho c on ic.codCarrinho = c.codCarrinho
                Inner Join Venda v on c.codVenda = v.codVenda
                Inner Join Usuario u on v.codUsuario = u.codUsuario
                Inner Join Produto p on ic.codProd = p.codProd
                where
                v.situacao = 'Em andamento' and v.codUsuario = @user
            ", conn);
            cmd.Parameters.AddWithValue("@user", user);
            var rd =  cmd.ExecuteReader();

            while (rd.Read()) 
            {
                carrinho.Add(new CartItem
                {
                    codCarrinho = rd.GetInt32("codCarrinho"),
                    codProd = rd.GetInt32("codProd"),
                    Quantidade = rd.GetInt32("quantidade"),
                    Valor = rd.GetDecimal("valorProduto")
                });
                produto.Add(new Produto
                {
                    nomeProduto = rd.GetString("nomeProduto")
                });
                venda.Add(new Venda
                {
                    codVenda = rd.GetInt32("codVenda")
                });

            }
            rd.Close();

            var cmd2 = new MySqlCommand(@"
                Select codCart, digitos, bandeira,tipoCart
                from Cartao_Clie
                where codUsuario = @usercode;
            ",conn);
            cmd2.Parameters.AddWithValue("@usercode", user);

            var rd2 = cmd2.ExecuteReader();

            while(rd2.Read())
            {
                cartao.Add(new cartaoCli
                {
                    codCart = rd2["codCart"] != DBNull.Value ? rd2.GetInt32("codCart") : 0,
                    digitos = rd2["digitos"] != DBNull.Value ? rd2.GetString("digitos") : null,
                    bandeira = rd2["bandeira"] != DBNull.Value ? rd2.GetString("bandeira") : null,
                    tipoCart = rd2["tipoCart"] != DBNull.Value ? rd2.GetString("tipoCart") : null
                });
            }

            ViewBag.Cartao = cartao;
            ViewBag.codVenda = venda;
            ViewBag.ProdutoNome = produto;
            return View(carrinho);
        }

        // ================================
        // SALVAR FORMA DE PAGAMENTO (ASYNC)
        // ================================

        [HttpPost("FormaPagamento")]
        public IActionResult AdicionarPagamento([FromBody] FormaPagamentoRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Forma))
                return Json(new { sucesso = false, mensagem = "Selecione uma forma de pagamento" });

            try
            {
                var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

                if (user == null || user == 0)
                    return Unauthorized();

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("inserir_formapagamento", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("u_formaPag", req.Forma);      // agora correto
                cmd.Parameters.AddWithValue("u_codCart", req.Codigo ?? 0); // se não tem cartão, salva 0
                cmd.Parameters.AddWithValue("u_codUsuario", user);

                cmd.ExecuteNonQuery();

                return Json(new { sucesso = true, mensagem = "Forma de pagamento salva!" });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro: " + ex.Message });
            }
        }

        // ===============================================
        // CONCLUIR COMPRA (ASYNC COMPLETO)
        // ===============================================
        [HttpPost("ConcluirCompra")]
        public async Task<IActionResult> ConcluirCompra([FromBody] ConcluirCompraRequest req)
        {
            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.TipoRetirada))
                return BadRequest("Tipo de retirada não informado");

            double frete = 0;
            EnderecoEntrega entrega = null;

            // ============================
            // SE FOR ENTREGA COM CEP
            // ============================
            if (req.TipoRetirada == "Entrega")
            {
                if (string.IsNullOrWhiteSpace(req.Cep))
                    return BadRequest("CEP obrigatório para entrega");

                string cepLimpo = req.Cep.Replace("-", "").Trim();

                // 1️⃣ Buscar endereço
                var endereco = await _enderecoService.ObterEnderecoPorCepAsync(cepLimpo);
                if (endereco == null)
                    return BadRequest("Não foi possível obter endereço pelo CEP");

                entrega = new EnderecoEntrega
                {
                    Bairro = endereco.Bairro,
                    Logradouro = endereco.Logradouro,
                    Cidade = endereco.Localidade,
                    Estado = endereco.Uf,
                    Cep = req.Cep
                };

                // 2️⃣ Calcular frete
                frete = await _freteService.CalcularFreteAsync("06343-010", cepLimpo);
            }

            // ===============================
            // CHAMA A PROCEDURE concluir_compra
            // ===============================
            string mensagem = "";
            double valorFinal = 0;
                       

            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using var cmd = new MySqlCommand("concluir_compra", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("u_cod", user);
                cmd.Parameters.AddWithValue("v_codigoCupom", req.Cupon ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("v_frete", frete);
                cmd.Parameters.AddWithValue("v_tipoRetirada", req.TipoRetirada);

                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    if (rd["Sucesso"] != DBNull.Value)
                        mensagem = rd["Sucesso"].ToString();

                    if (rd["ValorFinalComFrete"] != DBNull.Value)
                        valorFinal = Convert.ToDouble(rd["ValorFinalComFrete"]);
                }
            }


            TempData["Endereco"] = JsonSerializer.Serialize(entrega);
            // Retorna os dados para o JavaScript
            return Json(new
            {
                sucesso = true,
                mensagem,
                valorFinal,
                endereco = entrega
            });
        }




    }
    public class FormaPagamentoRequest
    {
        public string? Forma { get; set; }
        public int? Codigo { get; set; }
    }
}
