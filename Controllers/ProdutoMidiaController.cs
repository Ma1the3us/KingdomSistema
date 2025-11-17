using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics.Eventing.Reader;

namespace MeuProjetoMVC.Controllers
{
    public class ProdutoMidiaController : Controller
    {
        private readonly string _connectionString;

        public ProdutoMidiaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        public IActionResult Index(int? codProd)
        {
            List<ProdutoMidia> pm = new List<ProdutoMidia>();
            List<Produto> pr = new List<Produto>();
            var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
              select pm.codProd, pm.codMidia, pm.tipoMidia, pm.midia, pm.Ordem, p.nomeProduto from ProdutoMidia pm
              Inner Join Produto p on pm.codProd = p.codProd;
              where codProd = @cod;
              ", conn);
            cmd.Parameters.AddWithValue("@cod", codProd);
            var rd = cmd.ExecuteReader();

            while(rd.Read())
            {
                pm.Add(new ProdutoMidia
                {
                    codProd = rd.GetInt32("codProd"),
                    codMidia = rd.GetInt32("codMidia"),
                    midia = rd["midia"] != DBNull.Value ? (byte[])rd["midia"] : null,
                    tipoMidia = rd["tipoMidia"] as string,
                    Ordem = rd.GetInt32("Ordem")
                });

                pr.Add(new Produto
                {
                    codProd = rd.GetInt32("codProd"),
                    nomeProduto = rd.GetString("nomeProduto")
                });

            }

            ViewBag.Produtos = pr;

            return View(pm);

        }

        [HttpGet]
        public IActionResult Cadastrar(int? codProd)
        {
            return View(codProd);
        }

        [HttpPost]
        public IActionResult Cadastrar(ProdutoMidia produto, IFormFile midia)
        {
            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand("cad_midia_prod", conn) { CommandType = System.Data.CommandType.StoredProcedure };

                if (midia != null && midia.Length > 0)
                {
                    using var ms = new MemoryStream();
                    midia.CopyTo(ms);
                    produto.midia = ms.ToArray();
                }

                cmd.Parameters.AddWithValue("p_midia", produto.midia);
                cmd.Parameters.AddWithValue("p_cod", produto.codProd);
                cmd.Parameters.AddWithValue("p_tipomidia", produto.tipoMidia);
                cmd.ExecuteNonQuery();

                TempData["MensagemPI"] = "Midia atualizada com sucesso";
                return RedirectToAction("Index", "ProdutoMidia", produto.codProd);
            }
            catch(MySqlException ex)
            {
                TempData["MensagemEPI"] = "Erro ao realizar o cadastro" +ex;
                return RedirectToAction("Index", "ProdutoMidia", produto.codProd);
            }
            
        }

        [HttpGet]
        public IActionResult Editar(int? codMidia, int? codProd)
        {
            if (codMidia == null || codProd == null)
                return NotFound();

            var produto = new ProdutoMidia();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
            SELECT codMidia, codProd, tipoMidia, midia 
            FROM ProdutoMidia
            WHERE codMidia = @cod AND codProd = @codP;
            ", conn);

            cmd.Parameters.AddWithValue("@cod", codMidia);
            cmd.Parameters.AddWithValue("@codP", codProd);

            using var rd = cmd.ExecuteReader();

            if (rd.Read())
            {
                produto.codMidia = rd.GetInt32("codMidia");
                produto.codProd = rd.GetInt32("codProd");
                produto.tipoMidia = rd.GetString("tipoMidia");

                if (!rd.IsDBNull(rd.GetOrdinal("midia")))
                    produto.midia = (byte[])rd["midia"];
            }
            else
            {
                return NotFound();
            }

            return View(produto);
        }

        [HttpPost]
        public IActionResult Editar(ProdutoMidia produto, IFormFile midia)
        {
            if (produto == null)
            {
                return BadRequest();
            }

            try 
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();

                if (midia != null && midia.Length > 0)
                {
                    using var ms = new MemoryStream();
                    midia.CopyTo(ms);
                    produto.midia = ms.ToArray();
                }


                using var cmd = new MySqlCommand(@"
                Update ProdutoMidia     
                set tipoMidia = @tipomidia
                midia = @midia
                where = codMidia = @codM and codProd = @codP
            ", conn);
                cmd.Parameters.AddWithValue("@codM", produto.codMidia);
                cmd.Parameters.AddWithValue("@codP", produto.codProd);
                cmd.Parameters.AddWithValue("@midia", produto.midia);
                cmd.Parameters.AddWithValue("@tipomidia", produto.tipoMidia);
                cmd.ExecuteNonQuery();

                TempData["MensagemPI"] = "Midia do produto foi alterada com sucesso;";
                return RedirectToAction("Index","ProdutoMidia",produto.codProd);

            }
            catch(MySqlException ex)
            {
                TempData["MensagemEPI"] = "Erro ao realizar a edição:" + ex;
                return RedirectToAction("Index", "ProdutoMidia",produto.codProd);
            }      
        }

        public IActionResult Excluir(int? codMidia, int?codProd)
        {
            if(codMidia == 0)
            {
                TempData["mensagemEPI"] = "codigo da midia não encontrada";
                return RedirectToAction("Index", "ProdutoMidia", codProd);
            }

            if(codProd == 0)
            {
                TempData["mensagemEPI"] = "Código do produto não encontrado";
                return RedirectToAction("Index", "ProdutoMidia", codProd);
            }

            try
            {
                var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
                Delete from ProdutoMidia where codMidia =@codM and codProd = @codP;
                ");
                cmd.Parameters.AddWithValue("@codM", codMidia);
                cmd.Parameters.AddWithValue("@codP", codProd);
                cmd.ExecuteNonQuery();

                TempData["MensagemPI"] = "Midia deletada com sucesso";
                return RedirectToAction("Index", "ProdutoMidia", codProd);
            }
            catch( MySqlException ex)
            {
                TempData["MensagemEPI"] = "Erro ao realizar o acesso ao banco, erro:" + ex;
                return RedirectToAction("Index", "ProdutoMidia", codProd);
            }
            
        }



    }
}
