using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanLeave.Leave.Internal.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "leave");

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "leave",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkingDays = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequests_DateRange", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_LeaveRequests_WorkingDays", "[WorkingDays] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApproverId",
                schema: "leave",
                table: "LeaveRequests",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId_Status",
                schema: "leave",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "leave");
        }
    }
}
