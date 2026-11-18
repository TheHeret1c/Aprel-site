using CheZa_Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CheZa_Core.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Address { get; set; }
        public DbSet<Admin> Admin { get; set; }
        public DbSet<City> City { get;  set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Favourite> Favourite { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderItem> OrderItem { get; set; }
        public DbSet<OrderItemModifier> OrderItemModifier { get; set; }
        public DbSet<OrderStatus> OrderStatus { get; set; }
        public DbSet<PaymentType> PaymentType { get; set; }
        public DbSet<Review> Review { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<User> User { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Address>().ToTable("address");
            modelBuilder.Entity<Admin>().ToTable("admin");
            modelBuilder.Entity<City>().ToTable("city");
            modelBuilder.Entity<Customer>().ToTable("customer");
            modelBuilder.Entity<Favourite>().ToTable("favourite");
            modelBuilder.Entity<Order>().ToTable("order");
            modelBuilder.Entity<OrderItem>().ToTable("order_item");
            modelBuilder.Entity<OrderItemModifier>().ToTable("order_item_modifier");
            modelBuilder.Entity<OrderStatus>().ToTable("order_status");
            modelBuilder.Entity<PaymentType>().ToTable("payment_type");
            modelBuilder.Entity<Review>().ToTable("review");
            modelBuilder.Entity<Role>().ToTable("role");
            modelBuilder.Entity<User>().ToTable("user");

            modelBuilder.Entity<User>()
                .HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Admin)
                .WithOne(a => a.User)
                .HasForeignKey<Admin>(a => a.UserId);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.User)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Review)
                .WithOne(r => r.Customer)
                .HasForeignKey(r => r.CustomerId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Favourite)
                .WithOne(f => f.Customer)
                .HasForeignKey(f => f.CustomerId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Address)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Order)
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Review)
                .WithOne(r => r.Order)
                .HasForeignKey<Order>(r => r.OrderId);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItem)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<PaymentType>()
                .HasMany(pt => pt.Order)
                .WithOne(o => o.PaymentType)
                .HasForeignKey(o => o.PaymentTypeId);

            modelBuilder.Entity<OrderStatus>()
                .HasMany(os => os.Order)
                .WithOne(o => o.OrderStatus)
                .HasForeignKey(o => o.StatusId);

            modelBuilder.Entity<OrderItem>()
                .HasMany(oi => oi.OrderItemModifier)
                .WithOne(oim => oim.OrderItem)
                .HasForeignKey(oim => oim.OrderItemId);

            modelBuilder.Entity<City>()
                .HasMany(c => c.Address)
                .WithOne(a => a.City)
                .HasForeignKey(a => a.CityId);

            modelBuilder.Entity<Address>()
                .HasMany(a => a.Order)
                .WithOne(o => o.Address)
                .HasForeignKey(o => o.AddressId);

            modelBuilder.Entity<Address>()
                .Property(a => a.AddressId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Admin>()
                .Property(a => a.AdminId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<City>()
                .Property(c => c.CityId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Favourite>()
                .Property(f => f.FavoriteId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Order>()
                .Property(o => o.OrderId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.OrderItemId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<OrderItemModifier>()
                .Property(oim => oim.OrderItemModifierId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<OrderStatus>()
                .Property(os => os.StatusId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<PaymentType>()
                .Property(pt => pt.PaymentTypeId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Review>()
                .Property(r => r.ReviewId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<User>()
                .Property(u => u.UserId)
                .ValueGeneratedOnAdd();
        }
    }
}
