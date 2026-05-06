using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Services.Common;
using CloudCompute.Services.Gpu;
using CloudCompute.Services.Verification;
using CloudCompute.ViewModels.Gpus;
using CloudCompute.ViewModels.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Controllers;

[Authorize]
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

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Mine()
    {
        return View();
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

        TempData[GpuConstants.Status.TempDataMessageKey] = GpuConstants.Messages.GpuCreated;
        TempData[GpuConstants.Status.TempDataTypeKey] = "success";
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
}
