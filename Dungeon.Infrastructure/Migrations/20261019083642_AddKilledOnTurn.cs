using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKilledOnTurn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KilledOnTurn",
                table: "DungeonRunMobs",
                type: "integer",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "KilledOnTurn", table: "DungeonRunMobs");
        }
    }
}
