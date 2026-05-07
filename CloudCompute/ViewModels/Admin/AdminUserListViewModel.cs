using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Admin;

public class AdminUserListViewModel
{
    public AdminUserFilter Filter { get; set; } = new();

    public IReadOnlyList<AdminUserRow> Rows { get; set; } = Array.Empty<AdminUserRow>();

    public int TotalCount { get; set; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, Filter.PageSize)));
}

public class AdminUserRow
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwnerVerified { get; set; }
    public decimal CreditBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}
