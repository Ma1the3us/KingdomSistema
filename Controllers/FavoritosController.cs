using MeuProjetoMVC.Autenticacao;
using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace MeuProjetoMVC.Controllers
{
    [Route("sistema/favorito")]
    public class FavoritosController : Controller
    {
        private readonly string _connectionstring;

        public FavoritosController(IConfiguration iconfiguration)
        {
            _connectionstring = iconfiguration.GetConnectionString("DefaultConnection")
              ?? throw new ArgumentNullException("ConnectionString não encontrada");
        }
        
        // Variações de index, quando o int codUsuario estiver com valor, será porquê um funcionário entrou por fora na conta do cliente
        // Caso haja algum erro ou problema, mas caso o código de usuário seja 0 ou nulo, ele irá pegar o valor da conta e mostrar a página de favorito do respectivo usuário
        public IActionResult Index(int? codUsuario) //Ele vai pegar pela página do funcionário como opção de acesso a página
        {

            var conn = new MySqlConnection(_connectionstring);

            List<wishlist> favoritos = new List<wishlist>();
            List<Produto> produtos = new List<Produto>();

            // Se for o funcionário usando e for entrar na área de favoritos de outra pessoa, ele entra aqui
            if (codUsuario > 0)
            {
                conn.Open();

                using var cmd = new MySqlCommand(@"
                    Select w.codProd, w.codUsuario, p.nomeProduto, p.Imagens, p.Descricao, p.valor
                    from wishlist w
                    inner join Produto p on w.codProd = p.codProd
                    where codUsuario = @cod;
                ",conn);
                cmd.Parameters.AddWithValue("@cod", codUsuario);
                

                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    favoritos.Add(new wishlist
                    {
                        codProd = rd.GetInt32("codProd"),
                        codUsuario = rd.GetInt32("codUsuario")
                    });

                    produtos.Add(new Produto
                    {
                        codProd = rd.GetInt32("codProd"),
                        nomeProduto = rd.GetString("nomeProduto"),
                        Imagens = rd["Imagens"] != DBNull.Value ? (byte[])rd["Imagens"] : Array.Empty<byte>(),
                        Descricao = rd.GetString("Descricao"),
                        Valor = rd.GetDecimal("valor")
                    });
                }



            }
            else // Caso não seja funcionário ou o funcionário esteja entrando em seus favoritos, ele entra aqui.
            {
                conn.Open();
                
                var user = HttpContext.Session.GetInt32(SessionKeys.UserId);
        
                if(user == 0 || user == null)
                {
                    TempData["MensagemErro"] = "É necessário realizar login para entrar na página de favoritos";
                    return RedirectToAction("Index", "Home");
                }

                using var cmd = new MySqlCommand(@"
                    Select w.codProd, w.codUsuario, p.nomeProduto, p.Imagens, p.Descricao, p.valor
                    from wishlist w
                    inner join Produto p on w.codProd = p.codProd
                    where codUsuario = @cod;
                ", conn);
                cmd.Parameters.AddWithValue("@cod", user);

                var rd = cmd.ExecuteReader();

                while(rd.Read())
                {
                    favoritos.Add(new wishlist
                    {
                        codProd = rd.GetInt32("codProd"),
                        codUsuario = rd.GetInt32("codUsuario")
                    });

                    produtos.Add(new Produto
                    {
                        codProd = rd.GetInt32("codProd"),
                        nomeProduto = rd.GetString("nomeProduto"),
                        Imagens = rd["Imagens"] != DBNull.Value ? (byte[])rd["Imagens"] : Array.Empty<byte>(),
                        Descricao = rd.GetString("Descricao"),
                        Valor = rd.GetDecimal("valor")
                    });
                }
         
            }

            ViewBag.Produtos = produtos;

            return View(favoritos);

        }

        [HttpPost("adicionar")]
        public IActionResult AdicionarFavorito([FromBody] wishlist dados)
        {
            var codProd = dados.codProd;

            if(codProd == 0)
            {
                TempData["MensagemEFA"] = "Não foi possivel adicionar o produto aos favoritos";
                
            }

            try{
                
                var conn = new MySqlConnection(_connectionstring);
                conn.Open();



                var cod = HttpContext.Session.GetInt32(SessionKeys.UserId);

                using var cmd = new MySqlCommand("inserir_favorito", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_codProd", codProd);
                cmd.Parameters.AddWithValue("p_codUsuario", cod);
                cmd.ExecuteNonQuery();

                return Json(new { sucesso = true, mensagem = "Adicionado aos favoritos" });

            }
            catch(MySqlException ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao adicionar aos favoritos, erro:" + ex });
            }

        }

        [HttpPost("remover")]
        public IActionResult ExcluirFavoritoPagina([FromBody]wishlist dados)
        {
            var codProd = dados.codProd;

            if (codProd == 0)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao retirar produto dos favoritos" });
            }

            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if(user == 0 || user == null)
            {
                return Json(new { sucesso = false, mensagem = "Usuário precisa estar logado para adicionar aos favoritos" });
            }

            try
            {
                var conn = new MySqlConnection(_connectionstring);
                conn.Open();

                using var cmd = new MySqlCommand(@"
                Delete from wishlist where codProd = @codP and codUsuario = @codU
                ", conn);
                cmd.Parameters.AddWithValue("@codP", codProd);
                cmd.Parameters.AddWithValue("@codU", user);

                cmd.ExecuteNonQuery();

                return Json(new { sucesso = true, mensagem = "Produto removido com sucesso" });

            }
            catch(MySqlException ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao remover o produto. Erro:" + ex });
            }
        }



        [HttpPost]
        public IActionResult ExcluirFavoritoIndex( wishlist dados)
        {
            var codProd = dados.codProd;

            if (codProd == 0)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao retirar produto dos favoritos" });
            }

            var user = HttpContext.Session.GetInt32(SessionKeys.UserId);

            if (user == 0 || user == null)
            {
                return Json(new { sucesso = false, mensagem = "Usuário precisa estar logado para adicionar aos favoritos" });
            }

            try
            {
                var conn = new MySqlConnection(_connectionstring);
                conn.Open();

                using var cmd = new MySqlCommand(@"
                Delete from wishlist where codProd = @codP and codUsuario = @codU
                ", conn);
                cmd.Parameters.AddWithValue("@codP", codProd);
                cmd.Parameters.AddWithValue("@codU", user);

                cmd.ExecuteNonQuery();

                return View();

            }
            catch (MySqlException ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao remover o produto. Erro:" + ex });
            }
        }

    }
}
