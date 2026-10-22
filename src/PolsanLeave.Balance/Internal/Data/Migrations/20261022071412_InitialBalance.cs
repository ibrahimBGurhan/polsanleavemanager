using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanLeave.Balance.Internal.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "balance");

            migrationBuilder.CreateTable(
                name: "Adjustments",
                schema: "balance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Days = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "balance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    OpeningDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.CheckConstraint("CK_Settings_Singleton", "[Id] = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_EmployeeId",
                schema: "balance",
                table: "Adjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_Adjustments_OneOpeningBalancePerEmployee",
                schema: "balance",
                table: "Adjustments",
                column: "EmployeeId",
                unique: true,
                filter: "[Kind] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adjustments",
                schema: "balance");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "balance");
        }
    }
}
