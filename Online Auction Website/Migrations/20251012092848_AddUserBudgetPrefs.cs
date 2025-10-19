using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Auction_Website.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBudgetPrefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetCeiling",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ViewOnlyWhenOverBudget",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetCeiling",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ViewOnlyWhenOverBudget",
                table: "AspNetUsers");
        }
    }
}
