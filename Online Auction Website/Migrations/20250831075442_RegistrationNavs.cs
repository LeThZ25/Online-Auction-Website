using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationNavs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuctionRegistration_AspNetUsers_UserId",
                table: "AuctionRegistration");

            migrationBuilder.DropForeignKey(
                name: "FK_AuctionRegistration_Sessions_SessionId",
                table: "AuctionRegistration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuctionRegistration",
                table: "AuctionRegistration");

            migrationBuilder.RenameTable(
                name: "AuctionRegistration",
                newName: "Registrations");

            migrationBuilder.RenameIndex(
                name: "IX_AuctionRegistration_UserId",
                table: "Registrations",
                newName: "IX_Registrations_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuctionRegistration_SessionId_UserId",
                table: "Registrations",
                newName: "IX_Registrations_SessionId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Registrations",
                table: "Registrations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_AspNetUsers_UserId",
                table: "Registrations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Sessions_SessionId",
                table: "Registrations",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_AspNetUsers_UserId",
                table: "Registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Sessions_SessionId",
                table: "Registrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Registrations",
                table: "Registrations");

            migrationBuilder.RenameTable(
                name: "Registrations",
                newName: "AuctionRegistration");

            migrationBuilder.RenameIndex(
                name: "IX_Registrations_UserId",
                table: "AuctionRegistration",
                newName: "IX_AuctionRegistration_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Registrations_SessionId_UserId",
                table: "AuctionRegistration",
                newName: "IX_AuctionRegistration_SessionId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuctionRegistration",
                table: "AuctionRegistration",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuctionRegistration_AspNetUsers_UserId",
                table: "AuctionRegistration",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuctionRegistration_Sessions_SessionId",
                table: "AuctionRegistration",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
