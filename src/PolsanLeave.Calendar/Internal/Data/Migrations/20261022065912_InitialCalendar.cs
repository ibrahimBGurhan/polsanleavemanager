using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PolsanLeave.Calendar.Internal.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "calendar");

            migrationBuilder.CreateTable(
                name: "CalendarYears",
                schema: "calendar",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarYears", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                schema: "calendar",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Date);
                });

            migrationBuilder.InsertData(
                schema: "calendar",
                table: "CalendarYears",
                column: "Year",
                values: new object[]
                {
                    2026,
                    2027
                });

            migrationBuilder.InsertData(
                schema: "calendar",
                table: "PublicHolidays",
                columns: new[] { "Date", "Name" },
                values: new object[,]
                {
                    { new DateOnly(2026, 1, 1), "Yılbaşı" },
                    { new DateOnly(2026, 3, 20), "Ramazan Bayramı 1. gün" },
                    { new DateOnly(2026, 3, 21), "Ramazan Bayramı 2. gün" },
                    { new DateOnly(2026, 3, 22), "Ramazan Bayramı 3. gün" },
                    { new DateOnly(2026, 4, 23), "Ulusal Egemenlik ve Çocuk Bayramı" },
                    { new DateOnly(2026, 5, 1), "Emek ve Dayanışma Günü" },
                    { new DateOnly(2026, 5, 19), "Atatürk'ü Anma, Gençlik ve Spor Bayramı" },
                    { new DateOnly(2026, 5, 27), "Kurban Bayramı 1. gün" },
                    { new DateOnly(2026, 5, 28), "Kurban Bayramı 2. gün" },
                    { new DateOnly(2026, 5, 29), "Kurban Bayramı 3. gün" },
                    { new DateOnly(2026, 5, 30), "Kurban Bayramı 4. gün" },
                    { new DateOnly(2026, 7, 15), "Demokrasi ve Millî Birlik Günü" },
                    { new DateOnly(2026, 8, 30), "Zafer Bayramı" },
                    { new DateOnly(2026, 10, 29), "Cumhuriyet Bayramı" },
                    { new DateOnly(2027, 1, 1), "Yılbaşı" },
                    { new DateOnly(2027, 3, 9), "Ramazan Bayramı 1. gün" },
                    { new DateOnly(2027, 3, 10), "Ramazan Bayramı 2. gün" },
                    { new DateOnly(2027, 3, 11), "Ramazan Bayramı 3. gün" },
                    { new DateOnly(2027, 4, 23), "Ulusal Egemenlik ve Çocuk Bayramı" },
                    { new DateOnly(2027, 5, 1), "Emek ve Dayanışma Günü" },
                    { new DateOnly(2027, 5, 16), "Kurban Bayramı 1. gün" },
                    { new DateOnly(2027, 5, 17), "Kurban Bayramı 2. gün" },
                    { new DateOnly(2027, 5, 18), "Kurban Bayramı 3. gün" },
                    { new DateOnly(2027, 5, 19), "Kurban Bayramı 4. gün; Atatürk'ü Anma, Gençlik ve Spor Bayramı" },
                    { new DateOnly(2027, 7, 15), "Demokrasi ve Millî Birlik Günü" },
                    { new DateOnly(2027, 8, 30), "Zafer Bayramı" },
                    { new DateOnly(2027, 10, 29), "Cumhuriyet Bayramı" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarYears",
                schema: "calendar");

            migrationBuilder.DropTable(
                name: "PublicHolidays",
                schema: "calendar");
        }
    }
}
