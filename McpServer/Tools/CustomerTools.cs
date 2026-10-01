using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CustomerTools
{
    [McpServerTool]
    [Description(
        "Gets one customer by ID. Use only when the customer ID is already known. " +
        "Do not use after search_customer_by_name because its results already " +
        "contain complete customer details.")]
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
