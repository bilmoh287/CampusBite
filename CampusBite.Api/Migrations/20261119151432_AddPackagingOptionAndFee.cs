using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusBite.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingOptionAndFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PackagingFee",
                table: "Orders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PackagingOption",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TakeawayPackagingFee",
                table: "Cafes",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PackagingFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PackagingOption",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TakeawayPackagingFee",
                table: "Cafes");
        }
    }
}
