using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Econyx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeyConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EncryptedKey = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsConfigured = table.Column<bool>(type: "bit", nullable: false),
                    MaskedDisplay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyConfigurations_Provider",
                table: "ApiKeyConfigurations",
                column: "Provider",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyConfigurations");
        }
    }
}
