using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dungeon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunHeroIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "HeroIds",
                table: "DungeonRuns",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HeroIds", table: "DungeonRuns");
        }
    }
}
