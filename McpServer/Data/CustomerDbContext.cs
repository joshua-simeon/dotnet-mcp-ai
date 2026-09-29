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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}