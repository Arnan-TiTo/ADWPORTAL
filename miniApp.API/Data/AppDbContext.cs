using Microsoft.EntityFrameworkCore;
using miniApp.API.Models;

namespace miniApp.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationImage> LocationImages { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ProductBrand> ProductBrands { get; set; }
        public DbSet<OrderHd> OrderHd { get; set; }
        public DbSet<OrderDt> OrderDt { get; set; }
        public DbSet<ProductStock> ProductStocks { get; set; }
        public DbSet<UserLocation> UserLocations { get; set; }
        public DbSet<StockTransactions> StockTransactions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();


            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Location)
                .WithMany()
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductCategory>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserLocation>(e =>
            {
                e.ToTable("UserLocations");
                e.HasKey(x => new { x.UserId, x.LocationId });

                e.HasOne(x => x.User)
                 .WithMany(u => u.UserLocations)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Location)
                 .WithMany(l => l.UserLocations)
                 .HasForeignKey(x => x.LocationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}