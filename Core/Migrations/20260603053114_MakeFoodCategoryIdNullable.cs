using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class MakeFoodCategoryIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_foods_food_categories_category_id",
                table: "foods");

            migrationBuilder.AlterColumn<long>(
                name: "category_id",
                table: "foods",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "fk_foods_food_categories_category_id",
                table: "foods",
                column: "category_id",
                principalTable: "food_categories",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_foods_food_categories_category_id",
                table: "foods");

            migrationBuilder.AlterColumn<long>(
                name: "category_id",
                table: "foods",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_foods_food_categories_category_id",
                table: "foods",
                column: "category_id",
                principalTable: "food_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
