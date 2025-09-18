using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DartsScoring.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetScore = table.Column<int>(type: "int", nullable: false),
                    DoubleOut = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Legs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    LegNumber = table.Column<int>(type: "int", nullable: false),
                    StartingPlayerId = table.Column<int>(type: "int", nullable: false),
                    WinnerPlayerId = table.Column<int>(type: "int", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Legs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Legs_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Legs_Players_StartingPlayerId",
                        column: x => x.StartingPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Legs_Players_WinnerPlayerId",
                        column: x => x.WinnerPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CricketStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    N15 = table.Column<int>(type: "int", nullable: false),
                    N16 = table.Column<int>(type: "int", nullable: false),
                    N17 = table.Column<int>(type: "int", nullable: false),
                    N18 = table.Column<int>(type: "int", nullable: false),
                    N19 = table.Column<int>(type: "int", nullable: false),
                    N20 = table.Column<int>(type: "int", nullable: false),
                    BullMarks = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CricketStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CricketStates_Legs_LegId",
                        column: x => x.LegId,
                        principalTable: "Legs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CricketStates_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Turns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TurnNumber = table.Column<int>(type: "int", nullable: false),
                    TotalScored = table.Column<int>(type: "int", nullable: false),
                    WasBust = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turns_Legs_LegId",
                        column: x => x.LegId,
                        principalTable: "Legs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Turns_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Throws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TurnId = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    ScoreValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Throws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Throws_Turns_TurnId",
                        column: x => x.TurnId,
                        principalTable: "Turns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Matches",
                columns: new[] { "Id", "DoubleOut", "FinishedAt", "Mode", "StartedAt", "Status", "TargetScore" },
                values: new object[,]
                {
                    { 1, true, null, "X01", new DateTime(2024, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), "InProgress", 501 },
                    { 2, false, null, "Cricket", new DateTime(2024, 1, 1, 12, 30, 0, 0, DateTimeKind.Utc), "InProgress", 0 }
                });

            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "Id", "DisplayName" },
                values: new object[,]
                {
                    { 1, "Alice" },
                    { 2, "Bob" }
                });

            migrationBuilder.InsertData(
                table: "Legs",
                columns: new[] { "Id", "FinishedAt", "LegNumber", "MatchId", "StartingPlayerId", "WinnerPlayerId" },
                values: new object[,]
                {
                    { 1, null, 1, 1, 1, null },
                    { 2, null, 1, 2, 2, null }
                });

            migrationBuilder.InsertData(
                table: "MatchPlayers",
                columns: new[] { "Id", "MatchId", "Order", "PlayerId" },
                values: new object[,]
                {
                    { 1, 1, 0, 1 },
                    { 2, 1, 1, 2 },
                    { 3, 2, 0, 1 },
                    { 4, 2, 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "CricketStates",
                columns: new[] { "Id", "BullMarks", "LegId", "N15", "N16", "N17", "N18", "N19", "N20", "PlayerId", "Points" },
                values: new object[,]
                {
                    { 1, 0, 2, 0, 0, 0, 0, 0, 0, 1, 0 },
                    { 2, 0, 2, 0, 0, 0, 0, 0, 0, 2, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CricketStates_LegId_PlayerId",
                table: "CricketStates",
                columns: new[] { "LegId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CricketStates_PlayerId",
                table: "CricketStates",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_MatchId",
                table: "Legs",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_StartingPlayerId",
                table: "Legs",
                column: "StartingPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_WinnerPlayerId",
                table: "Legs",
                column: "WinnerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_MatchId_PlayerId",
                table: "MatchPlayers",
                columns: new[] { "MatchId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_PlayerId",
                table: "MatchPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Throws_TurnId",
                table: "Throws",
                column: "TurnId");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_LegId",
                table: "Turns",
                column: "LegId");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_PlayerId",
                table: "Turns",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CricketStates");

            migrationBuilder.DropTable(
                name: "MatchPlayers");

            migrationBuilder.DropTable(
                name: "Throws");

            migrationBuilder.DropTable(
                name: "Turns");

            migrationBuilder.DropTable(
                name: "Legs");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
