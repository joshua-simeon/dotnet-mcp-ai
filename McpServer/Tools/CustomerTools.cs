using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CustomerTools
{
    [McpServerTool]
    [Description("Gets customer information by customer ID.")]
    public static string GetCustomer(
        CustomerService customerService,
        [Description("The unique ID of the customer.")] int customerId)
    {
        var customer = customerService.GetCustomer(customerId);

        if (customer == null)
        {
            return $"Customer ID {customerId} was not found.";
        }

        return $"Customer ID: {customer.Id}, " +
               $"Name: {customer.Name}, " +
               $"Email: {customer.Email}";
    }
}
