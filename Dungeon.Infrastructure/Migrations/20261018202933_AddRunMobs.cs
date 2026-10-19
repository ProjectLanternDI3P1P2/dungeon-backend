using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunMobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TODO(tech-debt): existing runs get no mob rows and play their current floor
            // without mobs — backfill them from the generated floor if such runs must be kept.
            migrationBuilder.AddColumn<List<int>>(
                name: "DiscoveredRoomIds",
                table: "DungeonRuns",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'"
            );

            migrationBuilder.CreateTable(
                name: "DungeonRunMobs",
                columns: table => new
                {
                    ElementId = table.Column<int>(type: "integer", nullable: false),
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    HomeRoomId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsAggro = table.Column<bool>(type: "boolean", nullable: false),
                    Tick = table.Column<int>(type: "integer", nullable: false),
                    TeleportX = table.Column<int>(type: "integer", nullable: true),
                    TeleportY = table.Column<int>(type: "integer", nullable: true),
                    DoorX = table.Column<int>(type: "integer", nullable: true),
                    DoorY = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonRunMobs", x => new { x.DungeonRunId, x.ElementId });
                    table.ForeignKey(
                        name: "FK_DungeonRunMobs_DungeonRuns_DungeonRunId",
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
            migrationBuilder.DropTable(name: "DungeonRunMobs");

            migrationBuilder.DropColumn(name: "DiscoveredRoomIds", table: "DungeonRuns");
        }
    }
}
