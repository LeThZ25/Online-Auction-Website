using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgFieldsToSessionAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_CategoryId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status_StartUtc_EndUtc",
                table: "Sessions",
                columns: new[] { "Status", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId_CreatedAt",
                table: "Items",
                columns: new[] { "CategoryId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_Status_StartUtc_EndUtc",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Items_CategoryId_CreatedAt",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }
    }
}
