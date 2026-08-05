using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStoreWeb.Migrations
{
    public partial class AddOrderReceiverInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverPhone",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverEmail",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethod",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReceiverName", table: "Orders");
            migrationBuilder.DropColumn(name: "ReceiverPhone", table: "Orders");
            migrationBuilder.DropColumn(name: "ReceiverEmail", table: "Orders");
            migrationBuilder.DropColumn(name: "Note", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingMethod", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingFee", table: "Orders");
        }
    }
}
