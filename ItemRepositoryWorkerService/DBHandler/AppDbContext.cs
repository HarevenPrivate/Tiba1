using Microsoft.EntityFrameworkCore;

namespace ItemRepositoryWorkerService.DBHandler
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Item> Items => Set<Item>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id); // PK
                entity.HasIndex(u => u.UserName).IsUnique(); // unique index on username
                entity.Property(u => u.UserName).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
            });

            // Item entity
            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(i => i.Id); // PK

                entity.Property(i => i.Name).IsRequired();

                // Foreign key to User
                entity.HasOne(i => i.User)
                      .WithMany(u => u.Items) // collection navigation
                      .HasForeignKey(i => i.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // delete items when user deleted

                // Index for performance
                entity.HasIndex(i => i.UserId);
            });
        }
    }

    // User model with collection navigation
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = "User";

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }

    // Item model with navigation property
    public class Item
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!; // navigation property
        public string Name { get; set; } = null!;
        public bool IsDeleted { get; set; }
    }
}
