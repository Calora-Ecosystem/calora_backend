using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterTablesForBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_plan_extras_plan",
                table: "plan_extras");

            migrationBuilder.AddColumn<long>(
                name: "plan_extra_id",
                table: "subscription_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_orders_plan_extra_id",
                table: "subscription_orders",
                column: "plan_extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_extras_plan",
                table: "plan_extras",
                column: "plan");

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_orders_plan_extras_plan_extra_id",
                table: "subscription_orders",
                column: "plan_extra_id",
                principalTable: "plan_extras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscription_orders_plan_extras_plan_extra_id",
                table: "subscription_orders");

            migrationBuilder.DropIndex(
                name: "ix_subscription_orders_plan_extra_id",
                table: "subscription_orders");

            migrationBuilder.DropIndex(
                name: "ix_plan_extras_plan",
                table: "plan_extras");

            migrationBuilder.DropColumn(
                name: "plan_extra_id",
                table: "subscription_orders");

            migrationBuilder.CreateIndex(
                name: "ix_plan_extras_plan",
                table: "plan_extras",
                column: "plan",
                unique: true);
        }
    }
}
