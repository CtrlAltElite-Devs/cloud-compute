using CloudCompute.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users { get; set; }

    public DbSet<Gpu> Gpus { get; set; }

    public DbSet<Rental> Rentals { get; set; }

    public DbSet<CreditTransaction> CreditTransactions { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    public DbSet<OwnerVerificationRequest> OwnerVerificationRequests => Set<OwnerVerificationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(user => user.UserName)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Gpu>()
            .HasOne(gpu => gpu.Owner)
            .WithMany(user => user.Gpus)
            .HasForeignKey(gpu => gpu.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rental>()
            .HasIndex(rental => rental.ReferenceNumber)
            .IsUnique();

        modelBuilder.Entity<Rental>()
            .HasOne(rental => rental.Renter)
            .WithMany(user => user.RentalsAsRenter)
            .HasForeignKey(rental => rental.RenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rental>()
            .HasOne(rental => rental.Owner)
            .WithMany(user => user.RentalsAsOwner)
            .HasForeignKey(rental => rental.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rental>()
            .HasOne(rental => rental.Gpu)
            .WithMany(gpu => gpu.Rentals)
            .HasForeignKey(rental => rental.GpuId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditTransaction>()
            .HasOne(transaction => transaction.User)
            .WithMany(user => user.CreditTransactions)
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditTransaction>()
            .HasOne(transaction => transaction.Admin)
            .WithMany(user => user.AdminCreditTransactions)
            .HasForeignKey(transaction => transaction.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CreditTransaction>()
            .HasOne(transaction => transaction.RelatedRental)
            .WithMany(rental => rental.CreditTransactions)
            .HasForeignKey(transaction => transaction.RelatedRentalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(review => review.Renter)
            .WithMany(user => user.Reviews)
            .HasForeignKey(review => review.RenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(review => review.Gpu)
            .WithMany(gpu => gpu.Reviews)
            .HasForeignKey(review => review.GpuId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(review => review.Rental)
            .WithOne(rental => rental.Review)
            .HasForeignKey<Review>(review => review.RentalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OwnerVerificationRequest>()
            .HasOne(request => request.User)
            .WithMany(user => user.OwnerVerificationRequests)
            .HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
