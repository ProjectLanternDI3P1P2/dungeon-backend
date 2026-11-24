using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenedChests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenedChests",
                columns: table => new
                {
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    ElementId = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_OpenedChests",
                        x => new
                        {
                            x.DungeonRunId,
                            x.Floor,
                            x.ElementId,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_OpenedChests_DungeonRuns_DungeonRunId",
                        column: x => x.DungeonRunId,
                        principalTable: "DungeonRuns",
                        principalColumn: "DungeonRunId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OpenedChests");
        }
    }
}
