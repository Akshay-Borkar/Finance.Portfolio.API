using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Finance.PortfolioService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockSectorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SectorPE = table.Column<double>(type: "float", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ticker = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarketCap = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StockPE = table.Column<double>(type: "float", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockSectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_StockSectors_StockSectorId",
                        column: x => x.StockSectorId,
                        principalTable: "StockSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Investments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BuyingPrice = table.Column<double>(type: "float", nullable: false),
                    InvestmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StockDetailsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Investments_Stocks_StockDetailsId",
                        column: x => x.StockDetailsId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "StockSectors",
                columns: new[] { "Id", "CreatedBy", "DateCreated", "DateModified", "ModifiedBy", "SectorPE", "StockSectorName" },
                values: new object[,]
                {
                    { new Guid("9b1c2b4d-0d51-421b-ab51-8a2cd9e5f3f7"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2024, 2, 10, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 10, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), null, "IT" },
                    { new Guid("9b1c2b4d-0d51-421b-ab51-8a2cd9e5f3f8"), new Guid("00000000-0000-0000-0000-000000000000"), new DateTime(2024, 2, 10, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 10, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), null, "Health" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Investments_StockDetailsId",
                table: "Investments",
                column: "StockDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_StockSectorId",
                table: "Stocks",
                column: "StockSectorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Investments");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "StockSectors");
        }
    }
}
