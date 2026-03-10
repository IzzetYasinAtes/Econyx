using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Econyx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BalanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BalanceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BalanceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalPnLAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalPnLCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalPnLPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OpenPositionCount = table.Column<int>(type: "int", nullable: false),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ApiCostsAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApiCostsCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VolumeUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Spread = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedOutcome = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Outcomes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformOrderId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderPriceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OrderPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FilledPriceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    FilledPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    FilledQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketQuestion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EntryPriceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EntryPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentPriceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CurrentPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StrategyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExitPriceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ExitPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketQuestion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EntryAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EntryCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExitAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExitCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PnLAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PnLCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FeeCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StrategyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSnapshots_CreatedAt",
                table: "BalanceSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Markets_ExternalId_Platform",
                table: "Markets",
                columns: new[] { "ExternalId", "Platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Markets_Status",
                table: "Markets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MarketId",
                table: "Orders",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_IsOpen",
                table: "Positions",
                column: "IsOpen");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_MarketId",
                table: "Positions",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ClosedAt",
                table: "Trades",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_MarketId",
                table: "Trades",
                column: "MarketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalanceSnapshots");

            migrationBuilder.DropTable(
                name: "Markets");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
