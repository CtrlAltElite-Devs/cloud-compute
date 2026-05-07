using System.Security.Claims;
using CloudCompute.Services.Notifications;
using CloudCompute.ViewModels.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace CloudCompute.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationBellViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var raw = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(raw, out var userId))
        {
            return View(new NotificationBellViewModel { UnreadCount = 0 });
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return View(new NotificationBellViewModel { UnreadCount = count });
    }
}
