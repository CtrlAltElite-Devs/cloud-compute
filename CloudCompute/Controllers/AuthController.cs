using CloudCompute.Constants;
using CloudCompute.Services.Auth;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model);
            if (!result.Succeeded)
            {
                AddModelErrors(result);
                return View(model);
            }

            return RedirectToAction(
                AuthConstants.Redirects.DashboardIndexAction,
                AuthConstants.Redirects.DashboardController);
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View(new SignupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.SignupAsync(model);
            if (!result.Succeeded)
            {
                AddModelErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();

            return RedirectToAction(AuthConstants.Redirects.LoginAction);
        }

        [AllowAnonymous]
        [HttpGet(AuthConstants.Routes.AccessDeniedRoute)]
        public IActionResult AccessDenied()
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
}
