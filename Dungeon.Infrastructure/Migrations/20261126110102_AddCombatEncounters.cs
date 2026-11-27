using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InCombat",
                table: "DungeonRunMobs",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "SkipsNextTurn",
                table: "DungeonRunMobs",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.CreateTable(
                name: "DungeonRunEncounters",
                columns: table => new
                {
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    Trigger = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    Status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    BossElementId = table.Column<Guid>(type: "uuid", nullable: true),
                    CombatId = table.Column<Guid>(type: "uuid", nullable: true),
                    Outcome = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: true
                    ),
                    CreatedOnTurn = table.Column<int>(type: "integer", nullable: false),
                    ResolvedOnTurn = table.Column<int>(type: "integer", nullable: true),
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    MobElementIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonRunEncounters", x => x.EncounterId);
                    table.ForeignKey(
                        name: "FK_DungeonRunEncounters_DungeonRuns_DungeonRunId",
                        column: x => x.DungeonRunId,
                        principalTable: "DungeonRuns",
                        principalColumn: "DungeonRunId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DungeonRunParticipants",
                columns: table => new
                {
                    HeroId = table.Column<Guid>(type: "uuid", nullable: false),
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentHp = table.Column<int>(type: "integer", nullable: true),
                    CurrentMana = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_DungeonRunParticipants",
                        x => new { x.DungeonRunId, x.HeroId }
                    );
                    table.ForeignKey(
                        name: "FK_DungeonRunParticipants_DungeonRuns_DungeonRunId",
                        column: x => x.DungeonRunId,
                        principalTable: "DungeonRuns",
                        principalColumn: "DungeonRunId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DungeonRunEncounters_CombatId",
                table: "DungeonRunEncounters",
                column: "CombatId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DungeonRunEncounters_OneOpenPerRun",
                table: "DungeonRunEncounters",
                column: "DungeonRunId",
                unique: true,
                filter: "\"Status\" <> 'Resolved'"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DungeonRunEncounters");

            migrationBuilder.DropTable(name: "DungeonRunParticipants");

            migrationBuilder.DropColumn(name: "InCombat", table: "DungeonRunMobs");

            migrationBuilder.DropColumn(name: "SkipsNextTurn", table: "DungeonRunMobs");
        }
    }
}
