using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;

namespace MeuProjetoMVC.Controllers
{
    public class SubCategoriaController : Controller
    {
        private readonly string _connectionString;
        public IActionResult Index()
        {
            List<Sub_Categoria> Sub = new List<Sub_Categoria>();
            List<Categoria> cat = new List<Categoria>();
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("select codSub, nomeSubcategoria, codCat from Sub_Categoria", conn);
            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                Sub.Add(new Sub_Categoria
                {
                    nomeSubcategoria = rd["nomeSubcategoria"] as string,
                    codSub = rd.GetInt32("codSub"),
                    codCat = rd.GetInt32("codCat")
                });
            }

            conn.Close();

            ViewBag.Categoria = categorias();
    

            return View(Sub);
        }

        public IActionResult Cadastrar()
        {
            ViewBag.Categoria = categorias();
            return View();
        }

        [HttpPost]
        public IActionResult Cadastrar(Sub_Categoria sub)
        {
            try{
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("cad_subcat", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_nomesub", sub.nomeSubcategoria);
                cmd.Parameters.AddWithValue("p_cat", sub.codCat);
                cmd.ExecuteNonQuery();

                TempData["MensagemS"] = "Cadastro realizado com sucesso";
                return RedirectToAction(nameof(Index));
            }
            catch (MySqlException ex)
            {
                TempData["ErroS"] = "Erro ao realizar o cadastro"+ ex;
                return RedirectToAction(nameof(Index));
            }

        }

        [HttpGet]
        public IActionResult Editar(int codSub)
        {

            List<Sub_Categoria> cat = null;
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("Select codSub, nomeSubcategoria, codCat from Sub_Categoria where codSub = @cod");
            cmd.Parameters.AddWithValue("@cod", codSub);
            var rd = cmd.ExecuteReader();

            while(rd.Read())
            {
                cat.Add(new Sub_Categoria
                {
                    nomeSubcategoria = rd["nomeSubcategoria"] as string,
                    codCat = rd.GetInt32("codCat"),
                    codSub = rd.GetInt32("codSub")
                });
            }

            if(cat == null)
            {
                return BadRequest();
            }

            ViewBag.Categoria = categorias();

            return View(cat);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Editar(Sub_Categoria sub)
        {
            try{

                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                Update Sub_Categoria
                set nomeSubcategoria = @nome, codCat = @cod
                where codSub = @sub
            ");
                cmd.Parameters.AddWithValue("@cod", sub.codCat);
                cmd.Parameters.AddWithValue("@nome", sub.nomeSubcategoria);
                cmd.Parameters.AddWithValue("@sub", sub.codSub);
                cmd.ExecuteNonQuery();

                return RedirectToAction(nameof(Index));
            }
            catch (MySqlException ex)
            {
                TempData["ErroS"] = "Erro ao realizar a alteração da subCategoria:"+ sub.codSub + ex;
                return RedirectToAction(nameof(Index));
            }
            
        }

        public IActionResult Excluir(int codSub)
        {
            if(codSub != null)
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("Delete from Sub_Categoria where codSub = @cod");
                cmd.Parameters.AddWithValue("@cod", codSub);
                cmd.ExecuteNonQuery();

                TempData["MensagemS"] = "Subcategoria Excluida com sucesso";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErroS"] = "Erro, a subcategoria pode estar conectada a algum produto";
                return RedirectToAction(nameof(Index));
            }
            
        }


        public List<SelectListItem> categorias()
        {
            var lista = new List<SelectListItem>();

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("select codCat, nomeCategoria from Categorias", conn);
            var rd = cmd.ExecuteReader();

            while(rd.Read())
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
