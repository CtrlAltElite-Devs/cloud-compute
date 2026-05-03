using System.Diagnostics;
using CloudCompute.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers
{
    public class LandingController : Controller
    {
        public LandingController(ILogger<LandingController> logger) { }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
