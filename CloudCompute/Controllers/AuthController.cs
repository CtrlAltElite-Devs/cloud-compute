using CloudCompute.Constants;
using CloudCompute.Models.ViewModels.Auth;
using CloudCompute.Services.Auth;
using CloudCompute.Services.Common;
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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
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

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(AuthConstants.Redirects.LandingIndexAction, AuthConstants.Redirects.LandingController);
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
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

            return RedirectToAction(AuthConstants.Redirects.LandingIndexAction, AuthConstants.Redirects.LandingController);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();

            return RedirectToAction(AuthConstants.Redirects.LandingIndexAction, AuthConstants.Redirects.LandingController);
        }

        [HttpGet(AuthConstants.Routes.AccessDeniedRoute)]
        public IActionResult AccessDenied()
        {
            return StatusCode(StatusCodes.Status403Forbidden);
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
