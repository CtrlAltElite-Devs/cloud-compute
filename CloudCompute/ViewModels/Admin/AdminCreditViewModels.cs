using System.ComponentModel.DataAnnotations;
using CloudCompute.Constants;
using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Admin;

public class AdminGrantCreditViewModel
{
    [Required(ErrorMessage = "Choose a user.")]
    public Guid UserId { get; set; }

    public string? UserDisplay { get; set; }

    [Required]
    [Range(typeof(decimal), "1", "100000", ErrorMessage = "Amount must be between 1 and 100,000.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Please provide a reason.")]
    [StringLength(AdminConstants.Validation.MaxReasonLength, MinimumLength = AdminConstants.Validation.MinReasonLength,
        ErrorMessage = "Please provide between 5 and 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}

public class AdminBulkGrantViewModel
{
    [Required]
    [Range(typeof(decimal), "1", "100000", ErrorMessage = "Amount must be between 1 and 100,000.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Please provide a reason.")]
    [StringLength(AdminConstants.Validation.MaxReasonLength, MinimumLength = AdminConstants.Validation.MinReasonLength,
        ErrorMessage = "Please provide between 5 and 500 characters.")]
    public string Reason { get; set; } = string.Empty;

    public bool ActiveMembersOnly { get; set; } = true;
}

public class AdminRevokeCreditViewModel
{
    [Required]
    public Guid UserId { get; set; }

    public string? UserDisplay { get; set; }

    [Required]
    [Range(typeof(decimal), "1", "100000", ErrorMessage = "Amount must be between 1 and 100,000.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Please provide a reason.")]
    [StringLength(AdminConstants.Validation.MaxReasonLength, MinimumLength = AdminConstants.Validation.MinReasonLength,
        ErrorMessage = "Please provide between 5 and 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}

public class AdminCreditLedgerFilter
{
    public string? UserQuery { get; set; }
    public CreditTransactionType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AdminCreditLedgerViewModel
{
    public AdminCreditLedgerFilter Filter { get; set; } = new();
    public IReadOnlyList<AdminCreditLedgerRow> Rows { get; set; } = Array.Empty<AdminCreditLedgerRow>();
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, Filter.PageSize)));
}

public class AdminCreditLedgerRow
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplay { get; set; } = string.Empty;
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? AdminId { get; set; }
    public string? AdminDisplay { get; set; }
}
