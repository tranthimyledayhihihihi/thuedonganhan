using Microsoft.EntityFrameworkCore;
using THUEDONGANHAN.Models;

namespace THUEDONGANHAN.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SaleOrder> SaleOrders { get; set; }
        public DbSet<OTPVerification> OTPVerifications { get; set; } // ✅ v4.0: THÊM OTP
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // CONFIGURE RELATIONSHIPS
            // ============================================================

            // User - Student (1-to-many)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Student)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product - Owner (User)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductImage - Product
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Rental - Product
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Rentals)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rental - Renter (User)
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Renter)
                .WithMany(u => u.RentalsAsRenter)
                .HasForeignKey(r => r.RenterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment - Rental
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Rental)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment - Payer (User)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Payer)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review - Product
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review - User
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // RefreshToken - User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message - Sender (User)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message - Receiver (User)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message - Product (optional)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            // Message - Rental (optional)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Rental)
                .WithMany()
                .HasForeignKey(m => m.RentalId)
                .OnDelete(DeleteBehavior.SetNull);

            // Notification - User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ SaleOrder - Product
            modelBuilder.Entity<SaleOrder>()
                .HasOne(so => so.Product)
                .WithMany()
                .HasForeignKey(so => so.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ SaleOrder - Buyer (User)
            modelBuilder.Entity<SaleOrder>()
                .HasOne(so => so.Buyer)
                .WithMany()
                .HasForeignKey(so => so.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ v4.0: OTPVerification - User
            modelBuilder.Entity<OTPVerification>()
                .HasOne(otp => otp.User)
                .WithMany()
                .HasForeignKey(otp => otp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction - User
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // CONFIGURE TABLES WITH TRIGGERS (EF Core 7+)
            // ============================================================
            modelBuilder.Entity<User>().ToTable(tb => tb.HasTrigger("TR_Users_ValidateStudent"));
            modelBuilder.Entity<Rental>().ToTable(tb => tb.HasTrigger("TR_Rentals_RequireVerified"));
            modelBuilder.Entity<SaleOrder>().ToTable(tb => tb.HasTrigger("TR_SaleOrders_RequireVerified"));

            // ============================================================
            // CONFIGURE INDEXES
            // ============================================================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.StudentCode)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();

            // ============================================================
            // LƯU Ý: Không seed data ở đây
            // Dữ liệu được quản lý bằng SQL script: DATABASE_SETUP_COMPLETE.sql
            // ============================================================
        }
    }
}
