using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Econyx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenIdToPositionAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenId",
                table: "Positions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TokenId",
                table: "Orders");
        }
    }
}
