using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

[Authorize]
public class GpusController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Mine()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }
}
