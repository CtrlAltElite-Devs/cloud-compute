using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Auth;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

public class AdminController : Controller
{
    private readonly IAuthService _authService;

    public AdminController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.AdminLoginAsync(model);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            return View(model);
        }

        return RedirectToAction(AuthConstants.Redirects.AdminDashboardAction);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    public IActionResult Dashboard()
    {
        return View();
    }

    private void AddModelErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
