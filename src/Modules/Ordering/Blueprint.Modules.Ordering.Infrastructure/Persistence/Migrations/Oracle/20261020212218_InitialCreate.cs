using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blueprint.Modules.Ordering.Infrastructure.Persistence.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ORDERING");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "ORDERING",
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
                name: "Orders",
                schema: "ORDERING",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CustomerId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Currency = table.Column<string>(type: "NCHAR(3)", fixedLength: true, maxLength: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    Version = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "ORDERING",
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
                name: "OrderLines",
                schema: "ORDERING",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProductId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProductName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    OrderId = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ORDERING",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                schema: "ORDERING",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "ORDERING",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_OccurredOnUtc",
                schema: "ORDERING",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "ORDERING");

            migrationBuilder.DropTable(
                name: "OrderLines",
                schema: "ORDERING");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "ORDERING");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "ORDERING");
        }
    }
}
