using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Admin;
using CloudCompute.Services.Auth;
using CloudCompute.Services.Common;
using CloudCompute.Services.Verification;
using CloudCompute.ViewModels.Admin;
using CloudCompute.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly IVerificationService _verificationService;
    private readonly IAdminDashboardService _dashboardService;
    private readonly IAdminUserService _userService;
    private readonly IAdminCreditService _creditService;
    private readonly IAdminListingService _listingService;
    private readonly IAdminAnalyticsService _analyticsService;

    public AdminController(
        IAuthService authService,
        IVerificationService verificationService,
        IAdminDashboardService dashboardService,
        IAdminUserService userService,
        IAdminCreditService creditService,
        IAdminListingService listingService,
        IAdminAnalyticsService analyticsService)
    {
        _authService = authService;
        _verificationService = verificationService;
        _dashboardService = dashboardService;
        _userService = userService;
        _creditService = creditService;
        _listingService = listingService;
        _analyticsService = analyticsService;
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
    public async Task<IActionResult> Dashboard()
    {
        var vm = await _dashboardService.GetAsync();
        return View(vm);
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

    // ----- User Management -----

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> Users(AdminUserFilter filter)
    {
        var vm = await _userService.ListAsync(filter ?? new AdminUserFilter());
        return View("Users/Index", vm);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> UserDetail(Guid id)
    {
        var detail = await _userService.GetDetailAsync(id);
        if (detail is null)
        {
            SetAdminStatus(AdminConstants.Messages.UserNotFound, success: false);
            return RedirectToAction(nameof(Users));
        }

        return View("Users/Detail", detail);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendUser(Guid id)
    {
        var adminId = GetCurrentUserId();
        if (adminId is null) return Challenge();

        var result = await _userService.SetActiveAsync(id, isActive: false, actingAdminId: adminId.Value);
        SetAdminStatus(result, AdminConstants.Messages.UserSuspended);
        return RedirectToAction(nameof(UserDetail), new { id });
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateUser(Guid id)
    {
        var adminId = GetCurrentUserId();
        if (adminId is null) return Challenge();

        var result = await _userService.SetActiveAsync(id, isActive: true, actingAdminId: adminId.Value);
        SetAdminStatus(result, AdminConstants.Messages.UserReactivated);
        return RedirectToAction(nameof(UserDetail), new { id });
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleOwnerVerified(Guid id, bool isVerified)
    {
        var result = await _userService.SetOwnerVerifiedAsync(id, isVerified);
        var successMessage = isVerified
            ? AdminConstants.Messages.OwnerVerificationGranted
            : AdminConstants.Messages.OwnerVerificationRevoked;
        SetAdminStatus(result, successMessage);
        return RedirectToAction(nameof(UserDetail), new { id });
    }

    // ----- Credit Management -----

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> Credits(AdminCreditLedgerFilter filter)
    {
        var vm = await _creditService.GetLedgerAsync(filter ?? new AdminCreditLedgerFilter());
        return View("Credits/Index", vm);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> GrantCredits(Guid? userId)
    {
        var model = new AdminGrantCreditViewModel();
        if (userId.HasValue)
        {
            var info = await GetMemberDisplayAsync(userId.Value);
            if (info is not null)
            {
                model.UserId = info.Value.Id;
                model.UserDisplay = info.Value.Display;
                model.CurrentBalance = info.Value.Balance;
            }
        }

        return View("Credits/Grant", model);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantCredits(AdminGrantCreditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await ApplySelectedMemberAsync(model);
            return View("Credits/Grant", model);
        }

        var adminId = GetCurrentUserId();
        if (adminId is null) return Challenge();

        var result = await _creditService.GrantAsync(model, adminId.Value);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            await ApplySelectedMemberAsync(model);
            return View("Credits/Grant", model);
        }

        SetAdminStatus(AdminConstants.Messages.CreditsGranted, success: true);
        return RedirectToAction(nameof(Credits));
    }

    private async Task ApplySelectedMemberAsync(AdminGrantCreditViewModel model)
    {
        if (!model.UserId.HasValue) return;
        var info = await GetMemberDisplayAsync(model.UserId.Value);
        if (info is null) return;
        model.UserDisplay = info.Value.Display;
        model.CurrentBalance = info.Value.Balance;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> SearchMembers(string? q)
    {
        var results = await _userService.SearchMembersAsync(q, limit: 20);
        return Json(results.Select(r => new { id = r.Id, display = r.DisplayName, balance = r.Balance }));
    }

    private async Task<(Guid Id, string Display, decimal Balance)?> GetMemberDisplayAsync(Guid userId)
    {
        var detail = await _userService.GetDetailAsync(userId);
        if (detail is null) return null;
        return (detail.Id, $"{detail.FullName} · {detail.Email}", detail.CreditBalance);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public IActionResult BulkGrantCredits()
    {
        return View("Credits/BulkGrant", new AdminBulkGrantViewModel());
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkGrantCredits(AdminBulkGrantViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Credits/BulkGrant", model);
        }

        var adminId = GetCurrentUserId();
        if (adminId is null) return Challenge();

        var bulk = await _creditService.BulkGrantAsync(model, adminId.Value);
        if (!bulk.Result.Succeeded)
        {
            AddModelErrors(bulk.Result);
            return View("Credits/BulkGrant", model);
        }

        SetAdminStatus($"{AdminConstants.Messages.CreditsBulkGranted} ({bulk.AffectedUsers} user(s)).", success: true);
        return RedirectToAction(nameof(Credits));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> RevokeCredits(Guid? userId)
    {
        var model = new AdminRevokeCreditViewModel();
        if (userId.HasValue)
        {
            var info = await GetMemberDisplayAsync(userId.Value);
            if (info is not null)
            {
                model.UserId = info.Value.Id;
                model.UserDisplay = info.Value.Display;
                model.CurrentBalance = info.Value.Balance;
            }
        }

        return View("Credits/Revoke", model);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeCredits(AdminRevokeCreditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await ApplySelectedMemberAsync(model);
            return View("Credits/Revoke", model);
        }

        var adminId = GetCurrentUserId();
        if (adminId is null) return Challenge();

        var result = await _creditService.RevokeAsync(model, adminId.Value);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            await ApplySelectedMemberAsync(model);
            return View("Credits/Revoke", model);
        }

        SetAdminStatus(AdminConstants.Messages.CreditsRevoked, success: true);
        return RedirectToAction(nameof(Credits));
    }

    private async Task ApplySelectedMemberAsync(AdminRevokeCreditViewModel model)
    {
        if (!model.UserId.HasValue) return;
        var info = await GetMemberDisplayAsync(model.UserId.Value);
        if (info is null) return;
        model.UserDisplay = info.Value.Display;
        model.CurrentBalance = info.Value.Balance;
    }

    // ----- Listing Moderation -----

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> Listings(AdminListingFilter filter)
    {
        var vm = await _listingService.ListAsync(filter ?? new AdminListingFilter());
        return View("Listings/Index", vm);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> ListingDetail(Guid id)
    {
        var detail = await _listingService.GetDetailAsync(id);
        if (detail is null)
        {
            SetAdminStatus(AdminConstants.Messages.ListingNotFound, success: false);
            return RedirectToAction(nameof(Listings));
        }

        return View("Listings/Detail", detail);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveListing(Guid id)
    {
        var result = await _listingService.ApproveAsync(id);
        SetAdminStatus(result, AdminConstants.Messages.ListingApproved);
        return RedirectToAction(nameof(ListingDetail), new { id });
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectListing(Guid id, string? reason)
    {
        var result = await _listingService.RejectAsync(id, reason);
        SetAdminStatus(result, AdminConstants.Messages.ListingRejected);
        return RedirectToAction(nameof(ListingDetail), new { id });
    }

    // ----- Analytics -----

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> Analytics()
    {
        var vm = await _analyticsService.GetAsync();
        return View(vm);
    }

    // ----- Helpers -----

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

    private void SetAdminStatus(ServiceResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            SetAdminStatus(successMessage, success: true);
        }
        else
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? AdminConstants.Messages.SaveFailed;
            SetAdminStatus(message, success: false);
        }
    }

    private void SetAdminStatus(string message, bool success)
    {
        TempData[AdminConstants.Status.TempDataMessageKey] = message;
        TempData[AdminConstants.Status.TempDataTypeKey] = success
            ? AdminConstants.Status.Success
            : AdminConstants.Status.Danger;
    }
}
