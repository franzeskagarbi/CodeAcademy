using Microsoft.AspNetCore.Mvc;

namespace CodeAcademy.Controllers
{
    public class QuizController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
