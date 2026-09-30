using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class SearchCustomerByNameTools
{
    [McpServerTool]
    [Description("Searches customers by name and returns matching customers")]
    public static string SearchCustomerByName(
        CustomerService customerService,
        [Description("The full or partial customer name to search for.")] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Enter a customer name to search for.";
        }

        var customers = customerService.SearchCustomersByName(name.Trim());

        if (customers.Count == 0)
        {
            return $"No customers were found matching '{name}'.";
        }

        return string.Join(
            Environment.NewLine,
            customers.Select(customer =>
                $"Customer ID: {customer.Id}, Name: {customer.Name}, Email: {customer.Email}"));
    }
}