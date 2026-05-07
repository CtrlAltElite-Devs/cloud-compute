namespace CloudCompute.ViewModels.Notifications;

public class NotificationListViewModel
{
    public IReadOnlyList<NotificationItemViewModel> Items { get; init; } = Array.Empty<NotificationItemViewModel>();

    public int UnreadCount { get; init; }
}
