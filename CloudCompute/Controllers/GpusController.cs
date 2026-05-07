using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Gpu;
using CloudCompute.Services.Verification;
using CloudCompute.ViewModels.Gpus;
using CloudCompute.ViewModels.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Controllers;

[Authorize(Roles = nameof(UserRole.Member))]
public class GpusController : Controller
{
    private readonly IGpuService _gpuService;
    private readonly IVerificationService _verificationService;
    private readonly AppDbContext _dbContext;

    public GpusController(
        IGpuService gpuService,
        IVerificationService verificationService,
        AppDbContext dbContext)
    {
        _gpuService = gpuService;
        _verificationService = verificationService;
        _dbContext = dbContext;
    }

    [HttpGet("gpus")]
    public async Task<IActionResult> Index(string? search)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _gpuService.GetCatalogAsync(userId.Value, search);
        return View(model);
    }

    [HttpGet("gpus/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _gpuService.GetDetailAsync(userId.Value, id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    public async Task<IActionResult> Mine()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _gpuService.GetMineAsync(userId.Value);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var isOwnerVerified = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId.Value)
            .Select(user => (bool?)user.IsOwnerVerified)
            .FirstOrDefaultAsync();

        if (isOwnerVerified != true)
        {
            var status = await _verificationService.GetLatestRequestStatusAsync(userId.Value);
            return View("CreateLocked", new VerificationCtaViewModel { LatestRequestStatus = status });
        }

        return View(new GpuCreateViewModel { MinRentalHours = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GpuCreateViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _gpuService.CreateAsync(userId.Value, form);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            return View(form);
        }

        SetStatusMessage(GpuConstants.Messages.GpuCreated, success: true);
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost("gpus/{id:guid}/toggle-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _gpuService.ToggleStatusAsync(userId.Value, id);
        if (!result.Succeeded)
        {
            SetStatusMessage(FirstErrorMessage(result, GpuConstants.Messages.SaveFailed), success: false);
            return RedirectToAction(nameof(Mine));
        }

        var gpu = await _dbContext.Gpus.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        var message = gpu?.Status == Models.Enums.GpuStatus.Available
            ? GpuConstants.Messages.ListingResumed
            : GpuConstants.Messages.ListingPaused;

        SetStatusMessage(message, success: true);
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost("gpus/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _gpuService.DeleteAsync(userId.Value, id);
        if (!result.Succeeded)
        {
            SetStatusMessage(FirstErrorMessage(result, GpuConstants.Messages.SaveFailed), success: false);
            return RedirectToAction(nameof(Mine));
        }

        SetStatusMessage(GpuConstants.Messages.ListingDeleted, success: true);
        return RedirectToAction(nameof(Mine));
    }

    [HttpGet("gpus/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _gpuService.GetForEditAsync(userId.Value, id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("gpus/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, GpuEditViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        form.Id = id;

        if (!ModelState.IsValid)
        {
            var existing = await _gpuService.GetForEditAsync(userId.Value, id);
            form.ExistingImagePath = existing?.ExistingImagePath;
            form.Status = existing?.Status ?? form.Status;
            return View(form);
        }

        var result = await _gpuService.UpdateAsync(userId.Value, form);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            var existing = await _gpuService.GetForEditAsync(userId.Value, id);
            form.ExistingImagePath = existing?.ExistingImagePath;
            form.Status = existing?.Status ?? form.Status;
            return View(form);
        }

        SetStatusMessage(GpuConstants.Messages.ListingUpdated, success: true);
        return RedirectToAction(nameof(Mine));
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

    private void SetStatusMessage(string message, bool success)
    {
        TempData[GpuConstants.Status.TempDataMessageKey] = message;
        TempData[GpuConstants.Status.TempDataTypeKey] = success ? "success" : "danger";
    }

    private static string FirstErrorMessage(ServiceResult result, string fallback)
    {
        return result.Errors.FirstOrDefault()?.Message ?? fallback;
    }
}
