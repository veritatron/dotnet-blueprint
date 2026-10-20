using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blueprint.Modules.Catalog.Infrastructure.Persistence.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CATALOG");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "CATALOG",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    HandlerName = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.EventId, x.HandlerName });
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "CATALOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Type = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "NCLOB", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    Attempts = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "CATALOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Sku = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    StockQuantity = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Version = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Price = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "NCHAR(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                schema: "CATALOG",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                schema: "CATALOG",
                table: "Products",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "CATALOG");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "CATALOG");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "CATALOG");
        }
    }
}
