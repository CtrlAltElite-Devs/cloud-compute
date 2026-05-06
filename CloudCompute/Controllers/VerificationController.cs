using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Verification;
using CloudCompute.ViewModels.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

[Authorize]
public class VerificationController : Controller
{
    private readonly IVerificationService _verificationService;

    public VerificationController(IVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var page = await _verificationService.GetPageViewModelAsync(userId.Value);
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "Form")] RequestVerificationViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var page = await _verificationService.GetPageViewModelAsync(userId.Value);
        page.Form = model;

        if (!ModelState.IsValid)
        {
            return View(page);
        }

        var result = await _verificationService.SubmitRequestAsync(userId.Value, model);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            return View(page);
        }

        SetStatus(VerificationConstants.Messages.RequestSubmitted, success: true);
        return RedirectToAction(nameof(Index));
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

    private void SetStatus(string message, bool success)
    {
        TempData[VerificationConstants.Status.TempDataMessageKey] = message;
        TempData[VerificationConstants.Status.TempDataTypeKey] = success ? "success" : "danger";
    }
}
