using Microsoft.EntityFrameworkCore;
using McpServer.Models;

namespace McpServer.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .ToTable("MCPOrders")
            .Property(order => order.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                Name = "Alice Johnson",
                Email = "alice@example.com"
            },
            new Customer
            {
                Id = 2,
                Name = "Bob Smith",
                Email = "bob@example.com"
            },
            new Customer
            {
                Id = 3,
                Name = "Charlie Brown",
                Email = "charlie@example.com"
            }
        );

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = 1001,
                CustomerId = 1,
                Product = "Laptop",
                Amount = 1200m,
                Status = "Shipped"
            },
            new Order
            {
                Id = 1002,
                CustomerId = 2,
                Product = "Monitor",
                Amount = 450m,
                Status = "Delivered"
            },
            new Order
            {
                Id = 1003,
                CustomerId = 2,
                Product = "Keyboard",
                Amount = 120m,
                Status = "Shipped"
            },
            new Order
            {
                Id = 1004,
                CustomerId = 3,
                Product = "Mouse",
                Amount = 75m,
                Status = "Processing"
            }
        );
    }
}