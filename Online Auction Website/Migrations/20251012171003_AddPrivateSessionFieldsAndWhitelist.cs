using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateSessionFieldsAndWhitelist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HostId",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Sessions",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionWhitelists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AddedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionWhitelists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionWhitelists_AspNetUsers_AddedById",
                        column: x => x.AddedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionWhitelists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionWhitelists_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HostId",
                table: "Sessions",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWhitelists_AddedById",
                table: "SessionWhitelists",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWhitelists_SessionId_UserId",
                table: "SessionWhitelists",
                columns: new[] { "SessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionWhitelists_UserId",
                table: "SessionWhitelists",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_AspNetUsers_HostId",
                table: "Sessions",
                column: "HostId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_AspNetUsers_HostId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "SessionWhitelists");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_HostId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "HostId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Sessions");
        }
    }
}
