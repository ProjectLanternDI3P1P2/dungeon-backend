using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitedTiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "VisitedTiles",
                table: "DungeonRuns",
                type: "bytea",
                nullable: false,
                defaultValueSql: "'\\x'::bytea"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "VisitedTiles", table: "DungeonRuns");
        }
    }
}
