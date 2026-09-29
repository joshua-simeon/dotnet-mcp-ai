using McpServer.Models;

namespace McpServer.Repositories;

public interface ICustomerRepository
{
    Customer? GetCustomer(int customerId);
}