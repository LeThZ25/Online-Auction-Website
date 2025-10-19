using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetCodeToAuctionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetCode",
                table: "Items",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionHtml",
                table: "Items",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetCode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DescriptionHtml",
                table: "Items");
        }
    }
}
