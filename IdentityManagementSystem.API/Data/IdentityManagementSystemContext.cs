using Microsoft.EntityFrameworkCore;
using IdentityManagementSystem.API.Models;

namespace IdentityManagementSystem.API.Data
{
    public class IdentityManagementSystemContext : DbContext
    {
        public IdentityManagementSystemContext(DbContextOptions<IdentityManagementSystemContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }                  // جدید
        public DbSet<Request> Request { get; set; }
        public DbSet<Cartable> Cartable { get; set; }
        public DbSet<CartableItem> CartableItems { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<RequestHistory> RequestHistory { get; set; }
        public DbSet<RequestStatus> RequestStatus { get; set; }
        public DbSet<UserAccess> UserAccesses { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ShahkarLog> ShahkarLog { get; set; }
        public DbSet<VerifyDocLog> VerifyDocLog { get; set; }
        public DbSet<SmsLog> SmsLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ========== جداول و اسکیما ==========
            modelBuilder.Entity<User>().ToTable("Users", "Sec");
            modelBuilder.Entity<Role>().ToTable("Roles", "Sec");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles", "Sec");
            modelBuilder.Entity<Request>().ToTable("Request", "Define");
            modelBuilder.Entity<RequestStatus>().ToTable("RequestStatus", "Define");
            modelBuilder.Entity<Cartable>().ToTable("Cartable", "WF");
            modelBuilder.Entity<CartableItem>().ToTable("CartableItems", "WF");
            modelBuilder.Entity<UserLog>().ToTable("UserLog", "Log");          // توجه: در DB اسم جدول UserLog است (نه UserLogs)
            modelBuilder.Entity<RequestHistory>().ToTable("RequestHistory", "Sec");
            modelBuilder.Entity<UserAccess>().ToTable("UserAccess", "Sec");
            modelBuilder.Entity<Permission>().ToTable("Permissions", "Sec");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermissions", "Sec");
            modelBuilder.Entity<ShahkarLog>().ToTable("ShahkarLog", "Log");
            modelBuilder.Entity<VerifyDocLog>().ToTable("VerifyDocLog", "Log");
            modelBuilder.Entity<SmsLog>().ToTable("Sms", "Log");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens", "Sec");

            // ========== User ==========
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.HasIndex(u => u.NationalId).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();

                // Role و RoleId فقط NotMapped هستند، رابطه واقعی از طریق UserRoles است
                entity.Ignore(u => u.Role);
                entity.Ignore(u => u.RoleId);
            });

            // ========== UserRole (Many-to-Many) ==========
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => ur.UserRoleId);

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany()
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            });

            // ========== Role ==========
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.RoleId);
            });

            // ========== Request ==========
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(r => r.RequestId);
                entity.HasIndex(r => r.DocumentNumber);
                entity.HasIndex(r => r.NationalId);
                entity.HasIndex(r => r.RequestCode);
            });

            // ========== Cartable ==========
            modelBuilder.Entity<Cartable>(entity =>
            {
                entity.HasKey(c => c.CartableId);
                entity.HasIndex(c => c.UserId);

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== CartableItem ==========
            modelBuilder.Entity<CartableItem>(entity =>
            {
                entity.HasKey(ci => ci.ItemId);
                entity.HasIndex(ci => ci.CartableId);
                entity.HasIndex(ci => ci.RequestId);
                entity.HasIndex(ci => ci.AssignedTo);

                entity.HasOne(ci => ci.Cartable)
                      .WithMany()
                      .HasForeignKey(ci => ci.CartableId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ci => ci.Request)
                      .WithMany()
                      .HasForeignKey(ci => ci.RequestId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ci => ci.AssignedToUser)
                      .WithMany()
                      .HasForeignKey(ci => ci.AssignedTo)
                      .OnDelete(DeleteBehavior.SetNull);

                // فیلدهای NotMapped
                entity.Ignore(ci => ci.ValidateByExpert);
                entity.Ignore(ci => ci.Description);
            });

            // ========== UserLog ==========
            modelBuilder.Entity<UserLog>(entity =>
            {
                entity.HasKey(ul => ul.LogId);
                entity.HasIndex(ul => ul.UserId);
                entity.HasIndex(ul => ul.ActionTime);
            });

            // ========== RequestHistory ==========
            modelBuilder.Entity<RequestHistory>(entity =>
            {
                entity.HasKey(rh => rh.LogId);
                entity.HasIndex(rh => rh.RequestId);

                entity.HasOne(rh => rh.Request)
                      .WithMany()
                      .HasForeignKey(rh => rh.RequestId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rh => rh.Status)
                      .WithMany()
                      .HasForeignKey(rh => rh.StatusId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ExpertId از نوع string است (nvarchar در DB)
            });

            // ========== RequestStatus ==========
            modelBuilder.Entity<RequestStatus>(entity =>
            {
                entity.HasKey(rs => rs.StatusId);
            });

            // ========== UserAccess ==========
            modelBuilder.Entity<UserAccess>(entity =>
            {
                entity.HasKey(ua => ua.Id);
                entity.Property(ua => ua.Id).HasColumnName("AccessId");

                entity.HasIndex(ua => new { ua.UserId, ua.Permission }).IsUnique();

                entity.HasOne(ua => ua.User)
                      .WithMany()
                      .HasForeignKey(ua => ua.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ShahkarLog ==========
            modelBuilder.Entity<ShahkarLog>(entity =>
            {
                entity.HasKey(sl => sl.LogId);
                entity.HasIndex(sl => sl.RequestId);
                entity.HasIndex(sl => sl.ExpertId);
            });

            // ========== VerifyDocLog ==========
            modelBuilder.Entity<VerifyDocLog>(entity =>
            {
                entity.HasKey(v => v.VerifyDocLogId);
                entity.HasIndex(v => v.RequestId);
            });

            // ========== RefreshToken ==========
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.HasIndex(rt => rt.UserId);

                entity.HasOne(rt => rt.User)
                      .WithMany()
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}