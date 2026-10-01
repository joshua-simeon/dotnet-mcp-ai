using System.ComponentModel;
using ModelContextProtocol;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class SearchCustomerByNameTools
{
    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Searches customers by name and returns complete customer details, " +
        "including ID, name, and email. Do not call get_customer afterward " +
        "unless an additional lookup is explicitly required.")]
    public static CustomerResult[] SearchCustomerByName(
        CustomerService customerService,
        [Description("The full or partial customer name to search for.")] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new McpException("Enter a customer name to search for.");
        }

        var customers = customerService.SearchCustomersByName(name.Trim());

        if (customers.Count == 0)
        {
            throw new McpException($"No customers were found matching '{name}'.");
        }

        return customers
            .Select(customer => new CustomerResult(customer.Id, customer.Name, customer.Email))
            .ToArray();
    }
}
