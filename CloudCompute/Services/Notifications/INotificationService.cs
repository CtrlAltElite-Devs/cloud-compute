using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Notifications;

namespace CloudCompute.Services.Notifications;

public interface INotificationService
{
    /// <summary>
    /// Stages a new notification on the EF change tracker. The caller is responsible for invoking
    /// SaveChangesAsync (either directly or via an enclosing BeginTransactionAsync block) so the
    /// notification is persisted atomically with the operation that triggered it.
    /// </summary>
    void Create(Guid userId, NotificationType type, string message, string? link = null);

    Task<NotificationListViewModel> GetForUserAsync(Guid userId);

    Task<int> GetUnreadCountAsync(Guid userId);

    Task<ServiceResult> MarkAsReadAsync(Guid userId, Guid notificationId);

    Task<ServiceResult> MarkAllAsReadAsync(Guid userId);

    Task<NotificationOpenResult> OpenAsync(Guid userId, Guid notificationId);
}
