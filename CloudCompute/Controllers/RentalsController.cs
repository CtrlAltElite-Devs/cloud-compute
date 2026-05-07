using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Rentals;
using CloudCompute.Services.Reviews;
using CloudCompute.ViewModels.Rentals;

namespace CloudCompute.Controllers;

[Authorize(Roles = nameof(UserRole.Member))]
public class RentalsController : Controller
{
    private readonly IRentalService _rentalService;
    private readonly IReviewService _reviewService;

    public RentalsController(IRentalService rentalService, IReviewService reviewService)
    {
        _rentalService = rentalService;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Active()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _rentalService.GetActiveAsync(userId.Value);
        return View(model);
    }

    public async Task<IActionResult> History([FromQuery] RentalHistoryFilterViewModel filter)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _rentalService.GetHistoryAsync(userId.Value, filter);
        return View(model);
    }

    [HttpGet("rentals/receipt/{rentalId:guid}")]
    public async Task<IActionResult> Receipt(Guid rentalId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _rentalService.GetReceiptAsync(userId.Value, rentalId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("rentals/{rentalId:guid}/review")]
    public async Task<IActionResult> Review(Guid rentalId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (await _reviewService.GetFormAsync(userId.Value, rentalId) is null)
        {
            TempData[GpuConstants.Status.TempDataMessageKey] = "This rental cannot be reviewed.";
            TempData[GpuConstants.Status.TempDataTypeKey] = "danger";
            return RedirectToAction(nameof(History));
        }

        return RedirectToAction(nameof(History));
    }

    [HttpPost("rentals/{rentalId:guid}/review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(Guid rentalId, RentalReviewFormViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        form.RentalId = rentalId;

        if (!ModelState.IsValid)
        {
            TempData[GpuConstants.Status.TempDataMessageKey] = "Review rating must be between 1 and 5 stars, and comment must be 1000 characters or fewer.";
            TempData[GpuConstants.Status.TempDataTypeKey] = "danger";
            return RedirectToAction(nameof(History));
        }

        var result = await _reviewService.CreateAsync(userId.Value, form);
        if (!result.Succeeded)
        {
            TempData[GpuConstants.Status.TempDataMessageKey] = result.Errors.FirstOrDefault()?.Message ?? "This rental cannot be reviewed.";
            TempData[GpuConstants.Status.TempDataTypeKey] = "danger";
            return RedirectToAction(nameof(History));
        }

        TempData[GpuConstants.Status.TempDataMessageKey] = "Review submitted. Thanks for sharing your feedback.";
        TempData[GpuConstants.Status.TempDataTypeKey] = "success";
        return RedirectToAction(nameof(History));
    }

    [HttpGet("rentals/confirm/{gpuId:guid}")]
    public async Task<IActionResult> Confirm(Guid gpuId, int? durationHours)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _rentalService.GetConfirmationAsync(userId.Value, gpuId, durationHours);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("rentals/confirm/{gpuId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid gpuId, RentalConfirmViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var pageModel = await _rentalService.GetConfirmationAsync(userId.Value, gpuId, form.DurationHours);
        if (pageModel is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(pageModel);
        }

        var result = await _rentalService.CreateAsync(userId.Value, gpuId, form.DurationHours);
        if (!result.Succeeded)
        {
            AddModelErrors(result.ServiceResult);
            return View(pageModel);
        }

        TempData[GpuConstants.Status.TempDataMessageKey] = "Rental confirmed. Your GPU is now active.";
        TempData[GpuConstants.Status.TempDataTypeKey] = "success";
        return RedirectToAction(nameof(Active));
    }

    [HttpPost("rentals/terminate/{rentalId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(Guid rentalId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _rentalService.TerminateAsync(userId.Value, rentalId);
        if (!result.Succeeded)
        {
            TempData[GpuConstants.Status.TempDataMessageKey] = result.Errors.FirstOrDefault()?.Message ?? "Rental could not be terminated.";
            TempData[GpuConstants.Status.TempDataTypeKey] = "danger";
            return RedirectToAction(nameof(Active));
        }

        TempData[GpuConstants.Status.TempDataMessageKey] = "Rental terminated. The GPU is available again.";
        TempData[GpuConstants.Status.TempDataTypeKey] = "success";
        return RedirectToAction(nameof(Active));
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
}
