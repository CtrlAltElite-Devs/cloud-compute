using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Admin;

public class AdminUserFilter
{
    public string? Query { get; set; }

    public UserRole? Role { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsOwnerVerified { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
