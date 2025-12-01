using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

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

        // =========================
        // INDEX
        // =========================
        public IActionResult Index(int? codProd)
        {
            if (codProd == null)
                return BadRequest("Código do produto é obrigatório.");

            List<ProdutoMidia> pm = new();
            Produto produtoInfo = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
                SELECT 
                    pm.codProd, pm.codMidia, pm.tipoMidia, pm.midia, pm.Ordem,
                    p.nomeProduto
                FROM ProdutoMidia pm
                INNER JOIN Produto p ON pm.codProd = p.codProd
                WHERE pm.codProd = @cod;
            ", conn);

            cmd.Parameters.AddWithValue("@cod", codProd);

            using var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                pm.Add(new ProdutoMidia
                {
                    codProd = rd.GetInt32("codProd"),
                    codMidia = rd.GetInt32("codMidia"),
                    tipoMidia = rd["tipoMidia"] as string,
                    midia = rd["midia"] != DBNull.Value ? (string?)rd["midia"] : null,
                    Ordem = rd.GetInt32("Ordem")
                });

                if (produtoInfo == null)
                {
                    produtoInfo = new Produto
                    {
                        codProd = rd.GetInt32("codProd"),
                        nomeProduto = rd.GetString("nomeProduto")
                    };
                }
            }

            ViewBag.Produto = produtoInfo;
            ViewBag.codProd = codProd;
            return View(pm);
        }

        // =========================
        // CADASTRAR (GET)
        // =========================
        [HttpGet]
        public IActionResult Cadastrar(int codProd)
        {
            if (codProd == 0)
                return BadRequest("Código do produto não encontrado.");

            ViewBag.codProd = codProd;
            return View(new ProdutoMidia { codProd = codProd });
        }

        // =========================
        // CADASTRAR (POST)
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(ProdutoMidia produto, IFormFile midia)
        {
            if (midia != null && midia.Length > 0)
            {
                // Define a pasta correta baseado no tipo
                string pastaBase = produto.tipoMidia == "Imagem"
                    ? "imagens"
                    : "videos";

                // Cria uma pasta por produto (opcional, mas profissional)
                var pasta = Path.Combine("wwwroot", "midiaproduto", pastaBase, $"produto_{produto.codProd}");
                Directory.CreateDirectory(pasta);

                // Gera nome aleatório
                var fileName = $"{Guid.NewGuid()}_{midia.FileName}";
                var filePath = Path.Combine(pasta, fileName);

                // Salva fisicamente
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    midia.CopyTo(stream);
                }

                // Caminho para salvar no banco (URL)
                produto.midia = $"/midiaproduto/{pastaBase}/produto_{produto.codProd}/{fileName}";
            }

            // SALVAR NO BANCO
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("cad_midia_prod", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("p_midia", produto.midia);
            cmd.Parameters.AddWithValue("p_cod", produto.codProd);
            cmd.Parameters.AddWithValue("p_tipomidia", produto.tipoMidia);

            cmd.ExecuteNonQuery();

            TempData["MensagemPI"] = "Mídia cadastrada com sucesso!";

            return RedirectToAction("Index", new { codProd = produto.codProd });
        }

        // =========================
        // EDITAR (GET)
        // =========================
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
                WHERE codMidia = @codM AND codProd = @codP;
            ", conn);

            cmd.Parameters.AddWithValue("@codM", codMidia);
            cmd.Parameters.AddWithValue("@codP", codProd);

            using var rd = cmd.ExecuteReader();

            if (rd.Read())
            {
                produto.codMidia = rd.GetInt32("codMidia");
                produto.codProd = rd.GetInt32("codProd");
                produto.tipoMidia = rd.GetString("tipoMidia");
                produto.midia = !rd.IsDBNull(rd.GetOrdinal("midia"))  ? rd.GetString("midia") : null;
            }
            else
            {
                return NotFound();
            }

            ViewBag.codProd = codProd;
            return View(produto);
        }

        // =========================
        // EDITAR (POST)
        // =========================
        [HttpPost]
        public IActionResult Editar(ProdutoMidia produto, IFormFile midia)
        {
            if (!ModelState.IsValid)
                return View(produto);

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                if (midia != null && midia.Length > 0)
                {
                    // Define a pasta correta
                    string pastaBase = produto.tipoMidia == "Imagem"
                        ? "imagens"
                        : "videos";

                    // Pasta organizada por produto
                    var pasta = Path.Combine("wwwroot", "midiaproduto", pastaBase, $"produto_{produto.codProd}");
                    Directory.CreateDirectory(pasta);

                    // Nome do arquivo
                    var fileName = $"{Guid.NewGuid()}_{midia.FileName}";
                    var filePath = Path.Combine(pasta, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        midia.CopyTo(stream);
                    }

                    // Caminho salvo no banco
                    produto.midia = $"/uploads/{pastaBase}/produto_{produto.codProd}/{fileName}";
                }

                using var cmd = new MySqlCommand(@"
                   UPDATE ProdutoMidia
                   SET tipoMidia = @tipomidia,
                   midia = @midia
                   WHERE codMidia = @codM AND codProd = @codP;
                ", conn);

                cmd.Parameters.AddWithValue("@codM", produto.codMidia);
                cmd.Parameters.AddWithValue("@codP", produto.codProd);
                cmd.Parameters.AddWithValue("@midia", produto.midia);
                cmd.Parameters.AddWithValue("@tipomidia", produto.tipoMidia);

                cmd.ExecuteNonQuery();

                TempData["MensagemPI"] = "Mídia alterada com sucesso!";
                return RedirectToAction("Index", new { codProd = produto.codProd });
            }
            catch (MySqlException ex)
            {
                TempData["MensagemEPI"] = "Erro ao editar: " + ex.Message;
                return RedirectToAction("Index", new { codProd = produto.codProd });
            }
        }

        // =========================
        // EXCLUIR
        // =========================
        public IActionResult Excluir(int? codMidia, int? codProd)
        {
            if (codMidia == null)
            {
                TempData["mensagemEPI"] = "Código da mídia não encontrado.";
                return RedirectToAction("Index", new { codProd });
            }

            if (codProd == null)
            {
                TempData["mensagemEPI"] = "Código do produto não encontrado.";
                return RedirectToAction("Index", new { codProd });
            }

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var cmd = new MySqlCommand(@"
                    DELETE FROM ProdutoMidia 
                    WHERE codMidia = @codM AND codProd = @codP;
                ", conn);

                cmd.Parameters.AddWithValue("@codM", codMidia);
                cmd.Parameters.AddWithValue("@codP", codProd);

                cmd.ExecuteNonQuery();

                TempData["MensagemPI"] = "Mídia deletada com sucesso.";
                return RedirectToAction("Index", new { codProd });
            }
            catch (MySqlException ex)
            {
                TempData["MensagemEPI"] = "Erro ao excluir: " + ex.Message;
                return RedirectToAction("Index", new { codProd });
            }
        }

        public IActionResult Detalhes(int codMidia)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            ProdutoMidia midia = null;

            using (var cmd = new MySqlCommand("SELECT codMidia, codProd, tipoMidia, midia FROM ProdutoMidia WHERE codMidia = @codMidia;", conn))
            {
                cmd.Parameters.AddWithValue("@codMidia", codMidia);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        midia = new ProdutoMidia
                        {
                            codMidia = reader.GetInt32("codMidia"),
                            codProd = reader.GetInt32("codProd"),
                            tipoMidia = reader.GetString("tipoMidia"),
                            midia = reader["midia"] as string
                        };
                    }
                }
            }

            if (midia == null)
            {
                TempData["Erro"] = "Mídia não encontrada!";
                return RedirectToAction("Index");
            }

            return View(midia);
        }
    }
}
