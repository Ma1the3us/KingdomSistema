using MeuProjetoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text.Json;

namespace MeuProjetoMVC.Controllers
{
    public class ItemSubController : Controller
    {

        private readonly string _connectionString;

        public ItemSubController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string não encontrada");
        }

        public IActionResult Index(int? codProd)
        {
            var conn = new MySqlConnection(_connectionString);

            conn.Open();

            List<ItemSubcategoria> itemsub = new List<ItemSubcategoria>();

            if (codProd == 0 || codProd == null)
            {
                TempData["MensagemE"] = "Codigo de Produto não foi encontrado";
                return RedirectToAction("Detalhes", "Produto");
            }

            using var cmd = new MySqlCommand(@"
             select  codSub, codProd from Item_Subcategoria where codProd = @cod", conn);
            cmd.Parameters.AddWithValue("@cod", codProd);

            var rd = cmd.ExecuteReader();

            while(rd.Read())
            {
                itemsub.Add(new ItemSubcategoria
                {
                    codProd = rd.GetInt32("codProd"),
                    codSub = rd.GetInt32("codSub")
                });
            }

            Lista(codProd);

            return View(itemsub);
        
        }

        [HttpGet]
        public IActionResult Salvar()
        {
            List<Sub_Categoria> lista = new List<Sub_Categoria>();
            var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand(@"
              select codSub, nomeSubcategoria, codCat from Sub_Categoria 
                ", conn);
            var rd = cmd.ExecuteReader();
            while(rd.Read())
            {
                lista.Add(new Sub_Categoria
                {
                    codSub = rd.GetInt32("codSub"),
                    nomeSubcategoria = rd.GetString("nomeSubcategoria"),
                    codCat = rd.GetInt32("codCat")
                });
            }

            return View(lista);
        }

        [HttpPost]
        public IActionResult Salvar(string listaSelecionadas,ItemSubcategoria item)
        {
            var ids = JsonSerializer.Deserialize<List<int>>(listaSelecionadas);
            var conn = new MySqlConnection(_connectionString);
            conn.Open();

            if(ids.Count <= 0)
            {
                return BadRequest();
            }

            foreach (var id in ids)
            {
                using var cmd = new MySqlCommand("cad_itemSub", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("P_codSub", id);
                cmd.Parameters.AddWithValue("p_cod", item.codProd);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index","ItemSub",item.codProd);
        }


        public IActionResult Excluir(int? codProd, int? codSub)
        {
            var conn = new MySqlConnection(_connectionString);
            var cmd = new MySqlCommand(@"
                Delete from Item_Subcategoria where codProd = @codPr and codSub = @codSu;
                ", conn);
            cmd.Parameters.AddWithValue("@codPr", codProd);
            cmd.Parameters.AddWithValue("@codSu", codSub);

            return RedirectToAction("Index", "ItemSub", codProd);
        }

        public void Lista(int? codProd)
        {
            var conn = new MySqlConnection(_connectionString);

            List<Sub_Categoria> sub = new List<Sub_Categoria>();
            List<Produto> produto = new List<Produto>();

            conn.Open();

            using (var cmd = new MySqlCommand(@"
               select codSub, nomeSubcategoria, codCat ", conn))
            {
                var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    sub.Add(new Sub_Categoria
                    {
                        codSub = rd.GetInt32("codSub"),
                        codCat = rd.GetInt32("codCat"),
                        nomeSubcategoria = rd.GetString("nomeSubcategoria")
                    });
                }
            }

            using (var cmd = new MySqlCommand(@"
                select nomeProduto, codCat, codProd from Produto where @cod             
            ", conn))
            {
                var rd = cmd.ExecuteReader();

                while(rd.Read())
                {
                    produto.Add(new Produto
                    {
                        codProd = rd.GetInt32("codProd"),
                        codCat = rd.GetInt16("codCat"),
                        nomeProduto= rd.GetString("nomeProduo")
                    });
                }
            }


          ViewBag.Sub = sub;
          ViewBag.Produto = produto;
        }

    }
}
