using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionInvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SessionInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    InviterUserId = table.Column<string>(type: "TEXT", nullable: false),
                    InviteeUserId = table.Column<string>(type: "TEXT", nullable: true),
                    InviteeEmail = table.Column<string>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionInvites_AspNetUsers_InviteeUserId",
                        column: x => x.InviteeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionInvites_AspNetUsers_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionInvites_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_InviteeUserId",
                table: "SessionInvites",
                column: "InviteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_InviterUserId",
                table: "SessionInvites",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_SessionId",
                table: "SessionInvites",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionInvites_Token",
                table: "SessionInvites",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionInvites");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Sessions");
        }
    }
}
