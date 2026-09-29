using McpServer.Models;
using McpServer.Repositories;

namespace McpServer.Services;

public class CustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public Customer? GetCustomer(int customerId)
    {
        return _repository.GetCustomer(customerId);
    }
}