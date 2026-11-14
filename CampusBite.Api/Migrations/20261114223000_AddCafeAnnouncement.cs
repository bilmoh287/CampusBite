using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusBite.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCafeAnnouncement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Announcement",
                table: "Cafes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Announcement",
                table: "Cafes");
        }
    }
}
