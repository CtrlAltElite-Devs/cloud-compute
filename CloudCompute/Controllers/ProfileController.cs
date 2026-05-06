using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Services.Common;
using CloudCompute.Services.Profile;
using CloudCompute.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private const string StatusMessageKey = "ProfileStatusMessage";
    private const string StatusTypeKey = "ProfileStatusType";

    private readonly IProfileService _profileService;
    private readonly AppDbContext _dbContext;

    public ProfileController(IProfileService profileService, AppDbContext dbContext)
    {
        _profileService = profileService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        return View(BuildPageViewModel(user, profileForm: null, passwordForm: null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileEditViewModel model)
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), BuildPageViewModel(user, profileForm: model, passwordForm: null));
        }

        var result = await _profileService.UpdateProfileAsync(user.Id, model);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            return View(nameof(Index), BuildPageViewModel(user, profileForm: model, passwordForm: null));
        }

        SetStatus(AuthConstants.Profile.ProfileUpdated, success: true);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), BuildPageViewModel(user, profileForm: null, passwordForm: model));
        }

        var result = await _profileService.ChangePasswordAsync(user.Id, model);
        if (!result.Succeeded)
        {
            AddModelErrors(result);
            return View(nameof(Index), BuildPageViewModel(user, profileForm: null, passwordForm: model));
        }

        SetStatus(AuthConstants.Profile.PasswordUpdated, success: true);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _profileService.UpdateAvatarAsync(userId.Value, avatar);
        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? AuthConstants.Profile.AvatarRequired;
            SetStatus(message, success: false);
        }
        else
        {
            SetStatus(AuthConstants.Profile.AvatarUpdated, success: true);
        }

        return RedirectToAction(nameof(Index));
    }

    private static ProfilePageViewModel BuildPageViewModel(
        ApplicationUser user,
        ProfileEditViewModel? profileForm,
        ChangePasswordViewModel? passwordForm)
    {
        return new ProfilePageViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName,
            ProfilePicturePath = user.ProfilePicturePath,
            Profile = profileForm ?? new ProfileEditViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                Bio = user.Bio
            },
            Password = passwordForm ?? new ChangePasswordViewModel()
        };
    }

    private async Task<ApplicationUser?> LoadCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
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
        TempData[StatusMessageKey] = message;
        TempData[StatusTypeKey] = success ? "success" : "danger";
    }
}
