using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ProfilePicturePath { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditBalance { get; set; } = 500m;

    public bool IsOwnerVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public UserRole Role { get; set; } = UserRole.Member;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Gpu> Gpus { get; set; } = new List<Gpu>();

    public ICollection<Rental> RentalsAsRenter { get; set; } = new List<Rental>();

    public ICollection<Rental> RentalsAsOwner { get; set; } = new List<Rental>();

    public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();

    public ICollection<CreditTransaction> AdminCreditTransactions { get; set; } = new List<CreditTransaction>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<OwnerVerificationRequest> OwnerVerificationRequests { get; set; } = new List<OwnerVerificationRequest>();
}
