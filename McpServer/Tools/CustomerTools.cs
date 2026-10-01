using System.ComponentModel;
using ModelContextProtocol;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CustomerTools
{
    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Gets one customer by ID. Use only when the customer ID is already known. " +
        "Do not use after search_customer_by_name because its results already " +
        "contain complete customer details.")]
    public static CustomerResult GetCustomer(
        CustomerService customerService,
        [Description("The unique ID of the customer.")] int customerId)
    {
        var customer = customerService.GetCustomer(customerId);

        if (customer == null)
        {
            throw new McpException($"Customer ID {customerId} was not found.");
        }

        return new CustomerResult(customer.Id, customer.Name, customer.Email);
    }
}
