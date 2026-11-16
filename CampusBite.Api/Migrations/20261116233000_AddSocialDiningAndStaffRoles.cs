using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusBite.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialDiningAndStaffRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Username & EmployedAtCafeId to Users
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmployedAtCafeId",
                table: "Users",
                type: "int",
                nullable: true);

            // Backfill existing user accounts with a unique username based on email
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET Username = LOWER(REPLACE(SUBSTRING(Email, 1, CASE WHEN CHARINDEX('@', Email) > 1 THEN CHARINDEX('@', Email) - 1 ELSE LEN(Email) END), '.', '_'))
                WHERE Username = '' OR Username IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployedAtCafeId",
                table: "Users",
                column: "EmployedAtCafeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Cafes_EmployedAtCafeId",
                table: "Users",
                column: "EmployedAtCafeId",
                principalTable: "Cafes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 2. Add BaseMealCreditValue to ContractPlans
            migrationBuilder.AddColumn<decimal>(
                name: "BaseMealCreditValue",
                table: "ContractPlans",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 130.00m);

            // 3. Create BuddyConnections Table
            migrationBuilder.CreateTable(
                name: "BuddyConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterId = table.Column<int>(type: "int", nullable: false),
                    BuddyId = table.Column<int>(type: "int", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuddyConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuddyConnections_Users_BuddyId",
                        column: x => x.BuddyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuddyConnections_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuddyConnections_BuddyId",
                table: "BuddyConnections",
                column: "BuddyId");

            migrationBuilder.CreateIndex(
                name: "IX_BuddyConnections_RequesterId_BuddyId",
                table: "BuddyConnections",
                columns: new[] { "RequesterId", "BuddyId" },
                unique: true);

            // 4. Create OrderCoDiners Table
            migrationBuilder.CreateTable(
                name: "OrderCoDiners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentContractId = table.Column<int>(type: "int", nullable: true),
                    CreditsDeducted = table.Column<int>(type: "int", nullable: false),
                    UpgradeCashPortion = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCoDiners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCoDiners_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderCoDiners_StudentContracts_StudentContractId",
                        column: x => x.StudentContractId,
                        principalTable: "StudentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderCoDiners_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCoDiners_OrderId",
                table: "OrderCoDiners",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCoDiners_StudentContractId",
                table: "OrderCoDiners",
                column: "StudentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCoDiners_StudentId",
                table: "OrderCoDiners",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuddyConnections");

            migrationBuilder.DropTable(
                name: "OrderCoDiners");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Cafes_EmployedAtCafeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployedAtCafeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployedAtCafeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BaseMealCreditValue",
                table: "ContractPlans");
        }
    }
}
