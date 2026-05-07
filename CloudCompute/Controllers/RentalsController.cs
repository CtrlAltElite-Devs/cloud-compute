using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Rentals;
using CloudCompute.ViewModels.Rentals;

namespace CloudCompute.Controllers;

[Authorize(Roles = nameof(UserRole.Member))]
public class RentalsController : Controller
{
    private readonly IRentalService _rentalService;

    public RentalsController(IRentalService rentalService)
    {
        _rentalService = rentalService;
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

    public IActionResult History()
    {
        return View();
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
