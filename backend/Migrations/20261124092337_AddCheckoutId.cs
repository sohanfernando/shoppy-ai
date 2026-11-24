using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Orders_CustomerId", table: "Orders");
            migrationBuilder.AddColumn<Guid>(name: "CheckoutId", table: "Orders", type: "uniqueidentifier", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId_CheckoutId", table: "Orders", columns: new[] { "CustomerId", "CheckoutId" }, unique: true, filter: "[CheckoutId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Orders_CustomerId_CheckoutId", table: "Orders");
            migrationBuilder.DropColumn(name: "CheckoutId", table: "Orders");
            migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId", table: "Orders", column: "CustomerId");
        }
    }
}
