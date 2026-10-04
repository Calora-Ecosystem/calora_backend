using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNewSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "feature_key",
                table: "user_feature_usages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "feature_key",
                table: "plan_features",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<long>(
                name: "created_by_user_id",
                table: "coupons",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_coupons_created_by_user_id",
                table: "coupons",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_coupons_users_created_by_user_id",
                table: "coupons",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_coupons_users_created_by_user_id",
                table: "coupons");

            migrationBuilder.DropIndex(
                name: "ix_coupons_created_by_user_id",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "coupons");

            migrationBuilder.AlterColumn<string>(
                name: "feature_key",
                table: "user_feature_usages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "feature_key",
                table: "plan_features",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
