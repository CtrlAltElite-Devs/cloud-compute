namespace CloudCompute.ViewModels.Notifications;

public class NotificationBellViewModel
{
    public int UnreadCount { get; init; }

    public bool HasUnread => UnreadCount > 0;

    public string CountDisplay => UnreadCount > 99 ? "99+" : UnreadCount.ToString();
}
