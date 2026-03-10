using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Econyx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModelConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiModelConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    ContextLength = table.Column<int>(type: "int", nullable: false),
                    PromptPricePer1M = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CompletionPricePer1M = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModelConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiModelConfigurations_IsActive",
                table: "AiModelConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AiModelConfigurations_Provider_ModelId",
                table: "AiModelConfigurations",
                columns: new[] { "Provider", "ModelId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiModelConfigurations");
        }
    }
}
