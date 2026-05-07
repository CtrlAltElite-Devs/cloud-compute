using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Admin;

public class AdminUserDetailViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePicturePath { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwnerVerified { get; set; }
    public decimal CreditBalance { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<AdminUserCreditRow> RecentCredits { get; set; } = Array.Empty<AdminUserCreditRow>();

    public IReadOnlyList<AdminUserGpuRow> Gpus { get; set; } = Array.Empty<AdminUserGpuRow>();
}

public class AdminUserCreditRow
{
    public DateTime CreatedAt { get; set; }
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AdminUserGpuRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public GpuStatus Status { get; set; }
    public decimal PricePerHour { get; set; }
    public DateTime CreatedAt { get; set; }
}
