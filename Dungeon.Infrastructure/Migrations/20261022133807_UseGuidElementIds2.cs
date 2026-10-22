using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseGuidElementIds2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OpenedChests");
            migrationBuilder.DropTable(name: "DungeonRunMobs");
            migrationBuilder.CreateTable(
                name: "DungeonRunMobs",
                columns: table => new
                {
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false), Y = table.Column<int>(type: "integer", nullable: false),
                    HomeRoomId = table.Column<int>(type: "integer", nullable: false), IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    KilledOnTurn = table.Column<int>(type: "integer", nullable: true), IsAggro = table.Column<bool>(type: "boolean", nullable: false),
                    Tick = table.Column<int>(type: "integer", nullable: false), TeleportX = table.Column<int>(type: "integer", nullable: true),
                    TeleportY = table.Column<int>(type: "integer", nullable: true), DoorX = table.Column<int>(type: "integer", nullable: true), DoorY = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonRunMobs", x => new { x.DungeonRunId, x.ElementId });
                    table.ForeignKey("FK_DungeonRunMobs_DungeonRuns_DungeonRunId", x => x.DungeonRunId, "DungeonRuns", "DungeonRunId", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "OpenedChests",
                columns: table => new
                {
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: false), Floor = table.Column<int>(type: "integer", nullable: false),
                    ElementId = table.Column<Guid>(type: "uuid", nullable: false), OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenedChests", x => new { x.DungeonRunId, x.Floor, x.ElementId });
                    table.ForeignKey("FK_OpenedChests_DungeonRuns_DungeonRunId", x => x.DungeonRunId, "DungeonRuns", "DungeonRunId", onDelete: ReferentialAction.Cascade);
                });
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OpenedChests");
            migrationBuilder.DropTable(name: "DungeonRunMobs");
        }
    }
}
