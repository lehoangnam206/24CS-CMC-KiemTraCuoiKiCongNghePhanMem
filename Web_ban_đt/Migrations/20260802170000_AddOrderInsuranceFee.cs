using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStoreWeb.Migrations
{
    public partial class AddOrderInsuranceFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InsuranceFee", table: "Orders");
        }
    }
}
