using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceHunt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Searches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedSuppliers = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FromLocation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ToLocation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SearchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PriceMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    PriceCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierResponses_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Searches_StartedAt",
                table: "Searches",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_SearchId",
                table: "SupplierResponses",
                column: "SearchId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_SupplierName_RecordedAt",
                table: "SupplierResponses",
                columns: new[] { "SupplierName", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierResponses");

            migrationBuilder.DropTable(
                name: "Searches");
        }
    }
}
