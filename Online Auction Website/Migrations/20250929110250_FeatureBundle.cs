using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class FeatureBundle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_AspNetUsers_SellerId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Sessions_SessionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Bids_SessionId",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "PlacedAt",
                table: "Bids",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<int>(
                name: "AntiSnipingSeconds",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BidThrottleMs",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAntiSniping",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ExtendBySeconds",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtendCount",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtendMaxCount",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtendOnBidSeconds",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtendWindowSeconds",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "BLOB",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "SessionId",
                table: "Documents",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "Documents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProxyBids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AuctionSessionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyBids_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProxyBids_Sessions_AuctionSessionId",
                        column: x => x.AuctionSessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProxyBids_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => new { x.ItemId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Watchlists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Watchlists_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_News_CreatedAt",
                table: "News",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Items_AssetCode",
                table: "Items",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ItemId",
                table: "Documents",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_SessionId_CreatedAt",
                table: "Bids",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProxyBids_AuctionSessionId",
                table: "ProxyBids",
                column: "AuctionSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyBids_SessionId_UserId",
                table: "ProxyBids",
                columns: new[] { "SessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProxyBids_UserId",
                table: "ProxyBids",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_UserId_ItemId",
                table: "Watchlists",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Items_ItemId",
                table: "Documents",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_AspNetUsers_SellerId",
                table: "Items",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Sessions_SessionId",
                table: "Payments",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Items_ItemId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_AspNetUsers_SellerId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Sessions_SessionId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "ProxyBids");

            migrationBuilder.DropTable(
                name: "Watchlists");

            migrationBuilder.DropIndex(
                name: "IX_News_CreatedAt",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Items_AssetCode",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ItemId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Bids_SessionId_CreatedAt",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "AntiSnipingSeconds",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "BidThrottleMs",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EnableAntiSniping",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ExtendBySeconds",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ExtendCount",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ExtendMaxCount",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ExtendOnBidSeconds",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ExtendWindowSeconds",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Bids",
                newName: "PlacedAt");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "SessionId",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Documents",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Documents",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_SessionId",
                table: "Bids",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_AspNetUsers_SellerId",
                table: "Items",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Sessions_SessionId",
                table: "Payments",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
