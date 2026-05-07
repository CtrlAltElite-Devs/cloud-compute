using CloudCompute.Services.Common;

namespace CloudCompute.ViewModels.Notifications;

public sealed record NotificationOpenResult(bool Succeeded, string? Link, ServiceResult Result);
