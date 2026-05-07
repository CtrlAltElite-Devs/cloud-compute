using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.Controllers;

[Authorize(Roles = nameof(UserRole.Member))]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = await _notificationService.GetForUserAsync(userId.Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _notificationService.MarkAsReadAsync(userId.Value, id);
        if (result.Succeeded)
        {
            SetStatusMessage(NotificationConstants.Messages.MarkedAsRead, true);
        }
        else
        {
            SetStatusMessage(FirstErrorOr(result, NotificationConstants.Messages.SaveFailed), false);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
        if (result.Succeeded)
        {
            SetStatusMessage(NotificationConstants.Messages.AllMarkedAsRead, true);
        }
        else
        {
            SetStatusMessage(FirstErrorOr(result, NotificationConstants.Messages.SaveFailed), false);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _notificationService.OpenAsync(userId.Value, id);
        if (!result.Succeeded)
        {
            SetStatusMessage(FirstErrorOr(result.Result, NotificationConstants.Messages.SaveFailed), false);
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(result.Link) && Url.IsLocalUrl(result.Link))
        {
            return Redirect(result.Link);
        }

        return RedirectToAction(nameof(Index));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private void SetStatusMessage(string message, bool success)
    {
        TempData[NotificationConstants.Status.TempDataMessageKey] = message;
        TempData[NotificationConstants.Status.TempDataTypeKey] = success
            ? NotificationConstants.Status.Success
            : NotificationConstants.Status.Danger;
    }

    private static string FirstErrorOr(ServiceResult result, string fallback)
    {
        var error = result.Errors.FirstOrDefault();
        return error is null || string.IsNullOrWhiteSpace(error.Message) ? fallback : error.Message;
    }
}
