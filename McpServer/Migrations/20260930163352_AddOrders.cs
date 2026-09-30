using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace McpServer.Migrations
{
    /// <inheritdoc />
    public partial class AddOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MCPOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MCPOrders", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MCPOrders",
                columns: new[] { "Id", "Amount", "CustomerId", "Product", "Status" },
                values: new object[,]
                {
                    { 1001, 1200m, 1, "Laptop", "Shipped" },
                    { 1002, 450m, 2, "Monitor", "Delivered" },
                    { 1003, 120m, 2, "Keyboard", "Shipped" },
                    { 1004, 75m, 3, "Mouse", "Processing" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MCPOrders");
        }
    }
}
