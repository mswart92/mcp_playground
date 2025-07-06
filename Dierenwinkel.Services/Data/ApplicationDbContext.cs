using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Dierenwinkel.Services.Models;

namespace Dierenwinkel.Services.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            // Configure Order
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            // Configure relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(sci => sci.ShoppingCart)
                .WithMany(sc => sc.ShoppingCartItems)
                .HasForeignKey(sci => sci.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(sci => sci.Product)
                .WithMany(p => p.ShoppingCartItems)
                .HasForeignKey(sci => sci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ShoppingCart
            modelBuilder.Entity<ShoppingCart>()
                .HasIndex(sc => sc.SessionId);

            modelBuilder.Entity<ShoppingCart>()
                .HasIndex(sc => sc.UserId);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Premium Hondenvoer - Kip & Rijst",
                    Description = "Hoogwaardige droogvoer voor volwassen honden met kip en rijst. Rijk aan eiwitten en gemakkelijk verteerbaar.",
                    Price = 29.99m,
                    Category = "Hondenvoer",
                    StockQuantity = 50,
                    ImageUrl = "/images/products/dog-food-chicken-rice.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "Interactief Kattenspeelgoed - Muizenfeest",
                    Description = "Leuk interactief speelgoed voor katten met bewegende muizen die hun jachtinstinct stimuleren.",
                    Price = 15.99m,
                    Category = "Kattenspeelgoed",
                    StockQuantity = 30,
                    ImageUrl = "/images/products/cat-toy-mice.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Name = "Luxe Vogelkooi - Groot Model",
                    Description = "Ruime vogelkooi geschikt voor middelgrote vogels. Inclusief zitstokken en voer- en drinkbakjes.",
                    Price = 89.99m,
                    Category = "Vogelbenodigdheden",
                    StockQuantity = 15,
                    ImageUrl = "/images/products/bird-cage-large.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 4,
                    Name = "Aquarium Set - 60 Liter",
                    Description = "Complete aquarium set met filter, verwarming en LED-verlichting. Perfect voor beginners.",
                    Price = 129.99m,
                    Category = "Aquarium",
                    StockQuantity = 20,
                    ImageUrl = "/images/products/aquarium-60l.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 5,
                    Name = "Knaagdierenkooi - Hamster Palace",
                    Description = "Luxe kooi voor hamsters en andere kleine knaagdieren. Met meerdere verdiepingen en speelgoed.",
                    Price = 45.99m,
                    Category = "Knaagdierbenodigdheden",
                    StockQuantity = 25,
                    ImageUrl = "/images/products/hamster-cage.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 6,
                    Name = "Hondenspeelgoed - Rope Tug",
                    Description = "Stevige touwbal voor honden die graag trekken en kauwen. Goed voor de tandhygiëne.",
                    Price = 8.99m,
                    Category = "Hondenspeelgoed",
                    StockQuantity = 40,
                    ImageUrl = "/images/products/dog-rope-toy.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 7,
                    Name = "Kattenbak - Zelfreinigend",
                    Description = "Moderne zelfreiniggende kattenbak met automatische schoonmaakfunctie.",
                    Price = 199.99m,
                    Category = "Kattenaccessoires",
                    StockQuantity = 10,
                    ImageUrl = "/images/products/self-cleaning-litter-box.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 8,
                    Name = "Vogel Zaadmengsel - Deluxe",
                    Description = "Hoogwaardige zaadmengsel voor parkieten en kanaries. Met extra vitaminen en mineralen.",
                    Price = 12.99m,
                    Category = "Vogelvoer",
                    StockQuantity = 60,
                    ImageUrl = "/images/products/bird-seed-mix.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 9,
                    Name = "Aquarium Decoratie Set",
                    Description = "Mooie decoratie set voor aquariums met planten, rotsen en een scheepswrak.",
                    Price = 24.99m,
                    Category = "Aquarium",
                    StockQuantity = 35,
                    ImageUrl = "/images/products/aquarium-decoration.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 10,
                    Name = "Konijnenvoer - Pellets Premium",
                    Description = "Gebalanceerde pellets voor konijnen van alle leeftijden. Met extra vezels voor een goede spijsvertering.",
                    Price = 18.99m,
                    Category = "Konijnenvoer",
                    StockQuantity = 45,
                    ImageUrl = "/images/products/rabbit-pellets.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
