using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

[Authorize]
public class RentalsController : Controller
{
    public IActionResult Active()
    {
        return View();
    }

    public IActionResult History()
    {
        return View();
    }
}
