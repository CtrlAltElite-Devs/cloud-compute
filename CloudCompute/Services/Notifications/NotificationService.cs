using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;

    public NotificationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Create(Guid userId, NotificationType type, string message, string? link = null)
    {
        _dbContext.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Message = ClampMessage(message),
            Link = ClampLink(link),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<NotificationListViewModel> GetForUserAsync(Guid userId)
    {
        var items = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(NotificationConstants.Pagination.MaxItems)
            .Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                Link = n.Link,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        var unreadCount = await GetUnreadCountAsync(userId);

        return new NotificationListViewModel
        {
            Items = items,
            UnreadCount = unreadCount
        };
    }

    public Task<int> GetUnreadCountAsync(Guid userId)
    {
        return _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<ServiceResult> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is null)
        {
            return Fail(NotificationConstants.Messages.NotificationNotFound);
        }

        if (notification.IsRead)
        {
            return ServiceResult.Success();
        }

        notification.IsRead = true;
        try
        {
            await _dbContext.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            return Fail(NotificationConstants.Messages.SaveFailed);
        }
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            return Fail(NotificationConstants.Messages.SaveFailed);
        }
    }

    public async Task<NotificationOpenResult> OpenAsync(Guid userId, Guid notificationId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is null)
        {
            return new NotificationOpenResult(false, null, Fail(NotificationConstants.Messages.NotificationNotFound));
        }

        var link = notification.Link;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return new NotificationOpenResult(false, link, Fail(NotificationConstants.Messages.SaveFailed));
            }
        }

        return new NotificationOpenResult(true, link, ServiceResult.Success());
    }

    private static string ClampMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        if (message.Length <= NotificationConstants.Limits.MessageMaxLength)
        {
            return message;
        }

        return message.Substring(0, NotificationConstants.Limits.MessageMaxLength - 1) + "…";
    }

    private static string? ClampLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        if (link.Length > NotificationConstants.Limits.LinkMaxLength)
        {
            return null;
        }

        return link;
    }

    private static ServiceResult Fail(string message)
    {
        return ServiceResult.Failed(new ServiceError(AuthConstants.Validation.ModelErrorKey, message));
    }
}
