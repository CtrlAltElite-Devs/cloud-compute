using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers
{
    public class AuthController : Controller
    {
        // GET: AuthController
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Signup()
        {
            return View();
        }
    }
}
