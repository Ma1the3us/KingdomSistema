using Microsoft.AspNetCore.Mvc;

namespace MeuProjetoMVC.Controllers
{
    public class VendaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
