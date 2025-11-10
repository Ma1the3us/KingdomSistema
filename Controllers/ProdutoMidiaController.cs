using Microsoft.AspNetCore.Mvc;

namespace MeuProjetoMVC.Controllers
{
    public class ProdutoMidiaController : Controller
    {
        public IActionResult Index(int? codProd)
        {

            return View();

        }
    }
}
