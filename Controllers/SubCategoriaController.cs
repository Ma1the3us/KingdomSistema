using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

namespace MeuProjetoMVC.Controllers
{
    public class SubCategoriaController : Controller
    {
        private readonly string _connectionString;

        public SubCategoriaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        // ======================================================
        // INDEX — Lista subcategorias da categoria selecionada
        // ======================================================
        public IActionResult Index(int codCat)
        {
            if (codCat == 0)
                return BadRequest("Código da categoria não informado.");

            List<Sub_Categoria> Sub = new List<Sub_Categoria>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT codSub, nomeSubcategoria, codCat FROM Sub_Categoria WHERE codCat = @cod;", conn);
            cmd.Parameters.AddWithValue("@cod", codCat);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                Sub.Add(new Sub_Categoria
                {
                    codSub = rd.GetInt32("codSub"),
                    nomeSubcategoria = rd.GetString("nomeSubcategoria"),
                    codCat = rd.GetInt32("codCat")
                });
            }

            ViewBag.CodCat = codCat;              // mantém categoria atual
            ViewBag.Categorias = CategoriasListaPorCod(codCat); // usado em telas que suportam troca

            return View(Sub);
        }

        // ======================================================
        // CADASTRAR (GET)
        // ======================================================
        public IActionResult Cadastrar(int codCat)
        {
            if (codCat == 0)
                return BadRequest("Categoria não informada.");

            ViewBag.Categorias = CategoriasLista();
            ViewBag.CodCat = codCat;

            return View(new Sub_Categoria { codCat = codCat });
        }

        // ======================================================
        // CADASTRAR (POST)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastrar(Sub_Categoria sub)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = CategoriasLista();
                return View(sub);
            }

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand("cad_subcat", conn)
                { CommandType = System.Data.CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("p_nomesub", sub.nomeSubcategoria);
                cmd.Parameters.AddWithValue("p_cat", sub.codCat);
                cmd.ExecuteNonQuery();

                TempData["MensagemS"] = "Cadastro realizado com sucesso!";
                return RedirectToAction("Index", new { codCat = sub.codCat });
            }
            catch (MySqlException ex)
            {
                TempData["ErroS"] = "Erro ao realizar o cadastro: " + ex.Message;
                return RedirectToAction("Index", new { codCat = sub.codCat });
            }
        }

        // ======================================================
        // EDITAR (GET)
        // ======================================================
        [HttpGet]
        public IActionResult Editar(int codSub)
        {
            Sub_Categoria? sub = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT codSub, nomeSubcategoria, codCat FROM Sub_Categoria WHERE codSub = @cod;", conn);
            cmd.Parameters.AddWithValue("@cod", codSub);

            using var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                sub = new Sub_Categoria
                {
                    codSub = rd.GetInt32("codSub"),
                    nomeSubcategoria = rd.GetString("nomeSubcategoria"),
                    codCat = rd.GetInt32("codCat")
                };
            }

            if (sub == null)
                return NotFound();

            ViewBag.Categorias = CategoriasLista();
            ViewBag.CodCat = sub.codCat;

            return View(sub);
        }

        // ======================================================
        // EDITAR (POST)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Sub_Categoria sub)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = CategoriasLista();
                return View(sub);
            }

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand(@"
                    UPDATE Sub_Categoria 
                    SET nomeSubcategoria = @nome, codCat = @cat 
                    WHERE codSub = @sub;", conn);

                cmd.Parameters.AddWithValue("@nome", sub.nomeSubcategoria);
                cmd.Parameters.AddWithValue("@cat", sub.codCat);
                cmd.Parameters.AddWithValue("@sub", sub.codSub);
                cmd.ExecuteNonQuery();

                TempData["MensagemS"] = "Subcategoria atualizada!";
                return RedirectToAction("Index", new { codCat = sub.codCat });
            }
            catch (Exception ex)
            {
                TempData["ErroS"] = "Erro ao editar: " + ex.Message;
                return RedirectToAction("Index", new { codCat = sub.codCat });
            }
        }

        // ======================================================
        // EXCLUIR
        // ======================================================
        [HttpPost]
        public IActionResult Excluir(int codSub, int codCat)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var cmd = new MySqlCommand(
                    "DELETE FROM Sub_Categoria WHERE codSub = @sub AND codCat = @cat;", conn);

                cmd.Parameters.AddWithValue("@sub", codSub);
                cmd.Parameters.AddWithValue("@cat", codCat);
                cmd.ExecuteNonQuery();

                TempData["MensagemS"] = "Subcategoria excluída!";
                return RedirectToAction("Index", new { codCat });
            }
            catch
            {
                TempData["ErroS"] = "Erro ao excluir: a subcategoria pode estar vinculada a produtos.";
                return RedirectToAction("Index", new { codCat });
            }
        }

        // ======================================================
        // LISTA DE CATEGORIAS (SELECT)
        // ======================================================
        public List<SelectListItem> CategoriasLista()
        {
            var lista = new List<SelectListItem>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT codCat, nomeCategoria FROM Categorias;", conn);
            using var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                lista.Add(new SelectListItem
                {
                    Value = rd.GetInt32("codCat").ToString(),
                    Text = rd.GetString("nomeCategoria")
                });
            }

            return lista;
        }

        public List<SelectListItem> CategoriasListaPorCod(int? codCat)
        {
            var lista = new List<SelectListItem>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT codCat, nomeCategoria FROM Categorias where codCat = @cod;", conn);
            cmd.Parameters.AddWithValue("@cod", codCat);
            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                lista.Add(new SelectListItem
                {
                    Value = rd.GetInt32("codCat").ToString(),
                    Text = rd.GetString("nomeCategoria")
                });
            }

            return lista;
        }

    

    }
}
