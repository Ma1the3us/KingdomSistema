using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace MeuProjetoMVC.Controllers
{
    [Route("sistema/itemsub")]
    public class ItemSubController : Controller
    {
        private readonly string _connectionString;

        public ItemSubController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ======================================================
        // INDEX — Lista subcategorias vinculadas ao produto
        // ======================================================
        public IActionResult Index(int codProd)
        {
            if (codProd == 0)
            {
                TempData["MensagemE"] = "Código de Produto não informado";
                return RedirectToAction("Detalhes", "Produto");
            }

            List<ItemSubcategoria> itemsub = new List<ItemSubcategoria>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
                SELECT codSub, codProd 
                FROM Item_Subcategoria 
                WHERE codProd = @cod;", conn);

            cmd.Parameters.AddWithValue("@cod", codProd);

            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                itemsub.Add(new ItemSubcategoria
                {
                    codProd = rd.GetInt32("codProd"),
                    codSub = rd.GetInt32("codSub")
                });
            }

            ViewBag.CodProd = codProd;

            // Listas auxiliares (Subcategorias + Produto)
            CarregarSubcategorias();
            CarregarProduto(codProd);

            return View(itemsub);
        }

        // ======================================================
        // CADASTRAR (GET)
        // ======================================================
        [HttpGet]
        public IActionResult Cadastrar(int codProd)
        {
            if (codProd == 0)
                return BadRequest("Produto não informado.");

            ViewBag.CodProd = codProd;

            CarregarSubcategorias();

            return View(new ItemSubcategoria { codProd = codProd });
        }

        // ======================================================
        // CADASTRAR (POST NORMAL — formulário da View)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(ItemSubcategoria item)
        {
            if (!ModelState.IsValid)
            {
                CarregarSubcategorias();
                return View(item);
            }

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("cad_itemSub", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_codSub", item.codSub);
                cmd.Parameters.AddWithValue("p_cod", item.codProd);

                cmd.ExecuteNonQuery();

                TempData["MensagemS"] = "Subcategoria vinculada ao produto!";
                return RedirectToAction("Index", new { codProd = item.codProd });
            }
            catch (MySqlException ex)
            {
                TempData["ErroS"] = "Erro ao salvar: " + ex.Message;
                return RedirectToAction("Index", new { codProd = item.codProd });
            }
        }


        [HttpGet]
        public IActionResult Editar(int codProd)
        {
            if (codProd == 0)
                return BadRequest("Código do produto não informado.");

            var itemSub = new List<ItemSubcategoria>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            // Trazer subcategorias já selecionadas
            using (var cmd = new MySqlCommand(@"
            SELECT codProd, codSub 
            FROM  Item_Subcategoria 
            WHERE codProd = @cod;", conn))
            {
                cmd.Parameters.AddWithValue("@cod", codProd);
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    itemSub.Add(new ItemSubcategoria
                    {
                        codProd = rd.GetInt32("codProd"),
                        codSub = rd.GetInt32("codSub")
                    });
                }
            }

            // Trazer lista completa de subcategorias
            var subcategorias = new List<Sub_Categoria>();

            using (var cmd = new MySqlCommand(@"
            SELECT codSub, nomeSubcategoria, codCat 
            FROM Sub_Categoria;", conn))
            {
                var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    subcategorias.Add(new Sub_Categoria
                    {
                        codSub = rd.GetInt32("codSub"),
                        nomeSubcategoria = rd.GetString("nomeSubcategoria"),
                        codCat = rd.GetInt32("codCat")
                    });
                }
            }

            ViewBag.Subcategorias = subcategorias;
            ViewBag.codProd = codProd;
            ViewBag.Selecionadas = itemSub.Select(a => a.codSub).ToList();

            return View(itemSub);
        }

       
        [ValidateAntiForgeryToken]
        public IActionResult Editar(int codProd, List<int> codSubs)
        {
            if (codProd == 0)
                return BadRequest("Código do produto inválido.");

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // limpar tudo antes de salvar
                using (var cmd = new MySqlCommand(
                    "DELETE FROM Item_Subcategoria WHERE codProd = @p;", conn))
                {
                    cmd.Parameters.AddWithValue("@p", codProd);
                    cmd.ExecuteNonQuery();
                }

                // regravar
                foreach (var codSub in codSubs)
                {
                    using var cmd = new MySqlCommand("cad_itemSub", conn)
                    { CommandType = System.Data.CommandType.StoredProcedure };

                    cmd.Parameters.AddWithValue("p_codSub", codSub);
                    cmd.Parameters.AddWithValue("p_cod", codProd);
                    cmd.ExecuteNonQuery();
                }

                TempData["Mensagem"] = "Subcategorias atualizadas!";
                return RedirectToAction("Index", new { codProd });
            }
            catch (MySqlException ex)
            {
                TempData["Erro"] = "Erro ao editar: " + ex.Message;
                return RedirectToAction("Index", new { codProd });
            }
        }


        // ======================================================
        // CADASTRAR POR JSON (para Fetch)
        // ======================================================
        [HttpPost("salvar-por-json")]
        public IActionResult SalvarPorJson([FromBody] ItemSubJson payload)
        {
            if (payload == null)
                return BadRequest(new { sucesso = false, mensagem = "Payload inválido." });

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // 1️⃣ Apagar vínculos antigos
                using (var del = new MySqlCommand(
                    "DELETE FROM Item_Subcategoria WHERE codProd = @cod", conn))
                {
                    del.Parameters.AddWithValue("@cod", payload.codProd);
                    del.ExecuteNonQuery();
                }

                // 2️⃣ Inserir novos vínculos
                foreach (var codSub in payload.subSelecionados)
                {
                    using var ins = new MySqlCommand("cad_itemSub", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    ins.Parameters.AddWithValue("p_codSub", codSub);
                    ins.Parameters.AddWithValue("p_cod", payload.codProd);

                    ins.ExecuteNonQuery();
                }

                return Json(new { sucesso = true, mensagem = "Subcategorias atualizadas!" });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = ex.Message });
            }
        }

        // ======================================================
        // EXCLUIR
        // ======================================================
        public IActionResult Excluir(int codProd, int codSub)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
                DELETE FROM Item_Subcategoria 
                WHERE codProd = @codProd AND codSub = @codSub;", conn);

            cmd.Parameters.AddWithValue("@codProd", codProd);
            cmd.Parameters.AddWithValue("@codSub", codSub);

            cmd.ExecuteNonQuery();

            TempData["MensagemS"] = "Subcategoria removida!";

            return RedirectToAction("Index", new { codProd });
        }

        // ======================================================
        // AUXILIARES — listas para Views
        // ======================================================
        private void CarregarSubcategorias()
        {
            List<Sub_Categoria> lista = new List<Sub_Categoria>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
                SELECT codSub, nomeSubcategoria, codCat 
                FROM Sub_Categoria;", conn);

            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                lista.Add(new Sub_Categoria
                {
                    codSub = rd.GetInt32("codSub"),
                    nomeSubcategoria = rd.GetString("nomeSubcategoria"),
                    codCat = rd.GetInt32("codCat")
                });
            }

            ViewBag.Subcategorias = lista;
        }

        private void CarregarProduto(int codProd)
        {
            Produto? produto = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
                SELECT codProd, nomeProduto, codCat
                FROM Produto
                WHERE codProd = @cod;", conn);

            cmd.Parameters.AddWithValue("@cod", codProd);

            var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                produto = new Produto
                {
                    codProd = rd.GetInt32("codProd"),
                    nomeProduto = rd.GetString("nomeProduto"),
                    codCat = rd.GetInt32("codCat")
                };
            }

            ViewBag.Produto = produto;
        }

       
    }
    public class ItemSubJson
    {
        public int codProd { get; set; }
        public List<int> subSelecionados { get; set; } = new();
    }
}
