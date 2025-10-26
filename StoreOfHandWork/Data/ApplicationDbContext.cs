using Microsoft.EntityFrameworkCore;
using StoreOfHandWork.Models;

namespace StoreOfHandWork.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
            Users = Set<User>();
            Products = Set<Product>();
            Orders = Set<Order>();
            OrderItems = Set<OrderItem>();
            Categories = Set<Category>();
            Tags = Set<Tag>();
            Reviews = Set<Review>();
            WishListItems = Set<WishListItem>();
            CartItems = Set<CartItem>();
            ProductTags = Set<ProductTag>();
            ReturnRequests = Set<ReturnRequest>();
            ReturnPolicies = Set<ReturnPolicy>();
            PickupPoints = Set<PickupPoint>();
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            Users = Set<User>();
            Products = Set<Product>();
            Orders = Set<Order>();
            OrderItems = Set<OrderItem>();
            Categories = Set<Category>();
            Tags = Set<Tag>();
            Reviews = Set<Review>();
            WishListItems = Set<WishListItem>();
            CartItems = Set<CartItem>();
            ProductTags = Set<ProductTag>();
            ReturnRequests = Set<ReturnRequest>();
            ReturnPolicies = Set<ReturnPolicy>();
            PickupPoints = Set<PickupPoint>();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WishListItem> WishListItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ReturnPolicy> ReturnPolicies { get; set; }
        public DbSet<PickupPoint> PickupPoints { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=HOME-PC;Database=HandWorkDb;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка связи многие-ко-многим для ProductTag
            modelBuilder.Entity<ProductTag>(entity =>
            {
                // Явно указываем имя таблицы
                entity.ToTable("ProductTags");
                
                // Настраиваем составной ключ
                entity.HasKey(pt => new { pt.ProductId, pt.TagId });
                
                // Настраиваем связь с Product
                entity.HasOne(pt => pt.Product)
                      .WithMany()
                      .HasForeignKey(pt => pt.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                // Настраиваем связь с Tag
                entity.HasOne(pt => pt.Tag)
                      .WithMany()
                      .HasForeignKey(pt => pt.TagId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedDate).IsRequired()
                    .HasColumnType("datetime2");
                entity.Property(e => e.LastLoginDate).IsRequired()
                    .HasColumnType("datetime2");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.OrderDate)
                    .IsRequired()
                    .HasColumnType("datetime2");

                entity.Property(e => e.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PickupPoint)
                      .WithMany(pp => pp.Orders)
                      .HasForeignKey(e => e.PickupPointId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Quantity).IsRequired();

                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
                      
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // Конфигурация для ReturnRequest
            modelBuilder.Entity<ReturnRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestDate).IsRequired().HasColumnType("datetime2");
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Reason).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.AdditionalComments).HasMaxLength(1000);
                entity.Property(e => e.AdminComments).HasMaxLength(1000);
                entity.Property(e => e.ReviewDate).HasColumnType("datetime2");
                entity.Property(e => e.CompletionDate).HasColumnType("datetime2");

                entity.HasOne(e => e.OrderItem)
                      .WithMany()
                      .HasForeignKey(e => e.OrderItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ExchangeProduct)
                      .WithMany()
                      .HasForeignKey(e => e.ExchangeProductId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
            });

            // Конфигурация для ReturnPolicy
            modelBuilder.Entity<ReturnPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReturnPeriodDays).IsRequired();
                entity.Property(e => e.GeneralConditions).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.ExcludedCategories).HasMaxLength(2000);
                entity.Property(e => e.RefundPolicy).HasMaxLength(2000);
                entity.Property(e => e.ExchangePolicy).HasMaxLength(2000);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired().HasColumnType("datetime2");
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            });
        }
    }
}
