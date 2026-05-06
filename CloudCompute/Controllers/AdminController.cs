using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Auth;
using CloudCompute.Services.Common;
using CloudCompute.Services.Verification;
using CloudCompute.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly IVerificationService _verificationService;

    public AdminController(IAuthService authService, IVerificationService verificationService)
    {
        _authService = authService;
        _verificationService = verificationService;
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

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> Verifications()
    {
        var pending = await _verificationService.ListPendingAsync();
        return View(pending);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveVerification(Guid id, string? notes)
    {
        var adminId = GetCurrentUserId();
        if (adminId is null)
        {
            return Challenge();
        }

        var result = await _verificationService.ApproveAsync(id, adminId.Value, notes);
        SetVerificationStatus(result, VerificationConstants.Messages.RequestApproved);
        return RedirectToAction(nameof(Verifications));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectVerification(Guid id, string? notes)
    {
        var adminId = GetCurrentUserId();
        if (adminId is null)
        {
            return Challenge();
        }

        var result = await _verificationService.RejectAsync(id, adminId.Value, notes);
        SetVerificationStatus(result, VerificationConstants.Messages.RequestRejected);
        return RedirectToAction(nameof(Verifications));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private void AddModelErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }

    private void SetVerificationStatus(ServiceResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            TempData[VerificationConstants.Status.TempDataMessageKey] = successMessage;
            TempData[VerificationConstants.Status.TempDataTypeKey] = "success";
        }
        else
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? VerificationConstants.Messages.RequestNotFound;
            TempData[VerificationConstants.Status.TempDataMessageKey] = message;
            TempData[VerificationConstants.Status.TempDataTypeKey] = "danger";
        }
    }
}
