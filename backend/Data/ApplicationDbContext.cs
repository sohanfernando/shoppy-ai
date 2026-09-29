using AdvancedOrderSystem.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Data;

// Users, roles, logins and tokens come from Identity (the AspNet* tables)
public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // AppUser
        // =========================

        // Identity configures the rest of this table
        modelBuilder.Entity<AppUser>()
            .Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        // =========================
        // Product
        // =========================

        modelBuilder.Entity<Product>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<Product>()
           .Property(p => p.Name)
           .IsRequired()
           .HasMaxLength(150);

        modelBuilder.Entity<Product>()
            .Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SKU)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .Property(p => p.UnitPrice)
            .HasPrecision(18, 2);

        // =========================
        // Customer
        // =========================

        modelBuilder.Entity<Customer>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Customer>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder.Entity<Customer>()
            .Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // One sign-in account belongs to at most one customer
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.AppUser)
            .WithOne()
            .HasForeignKey<Customer>(c => c.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.AppUserId)
            .IsUnique()
            .HasFilter("[AppUserId] IS NOT NULL");

        // =========================
        // Order
        // =========================

        modelBuilder.Entity<Order>()
            .HasKey(o => o.Id);

        modelBuilder.Entity<Order>()
            .Property(o => o.Subtotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50);


        // Customer 1 -> Many Orders
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // OrderItem
        // =========================

        modelBuilder.Entity<OrderItem>()
            .HasKey(oi => oi.Id);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.LineTotal)
            .HasPrecision(18, 2);

        // Order 1 -> Many OrderItems
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Product 1 -> Many OrderItems
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // Review
        // =========================

        modelBuilder.Entity<Review>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<Review>()
            .Property(r => r.Comment)
            .IsRequired()
            .HasMaxLength(Review.CommentMaxLength);

        // Product 1 -> Many Reviews
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Customer 1 -> Many Reviews
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // One review per customer per product
        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.ProductId, r.CustomerId })
            .IsUnique();

        // =========================
        // Notification
        // =========================

        modelBuilder.Entity<Notification>()
            .HasKey(n => n.Id);

        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(50);

        modelBuilder.Entity<Notification>()
            .Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder.Entity<Notification>()
            .Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        modelBuilder.Entity<Notification>()
            .Property(n => n.Link)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientUserId, n.IsRead });

        // =========================
        // Seed Products
        // =========================

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Laptop",
                SKU = "LAP-001",
                UnitPrice = 150000.00m,
                Stock = 10,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Product
            {
                Id = 2,
                Name = "Mouse",
                SKU = "MOU-001",
                UnitPrice = 2500.00m,
                Stock = 25,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Product
            {
                Id = 3,
                Name = "Keyboard",
                SKU = "KEY-001",
                UnitPrice = 5000.00m,
                Stock = 20,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Product
            {
                Id = 4,
                Name = "Monitor",
                SKU = "MON-001",
                UnitPrice = 45000.00m,
                Stock = 8,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Product
            {
                Id = 5,
                Name = "Headphones",
                SKU = "HEA-001",
                UnitPrice = 7500.00m,
                Stock = 15,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );

        // =========================
        // Seed Customers
        // =========================

        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                Name = "John Silva",
                Email = "john@example.com",
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Customer
            {
                Id = 2,
                Name = "Kasun Perera",
                Email = "kasun@example.com",
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Customer
            {
                Id = 3,
                Name = "Nimal Fernando",
                Email = "nimal@example.com",
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );
    }
}