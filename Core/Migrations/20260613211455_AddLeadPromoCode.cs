using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadPromoCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "coupon_id",
                table: "leads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "promo_code",
                table: "leads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "coupon_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "promo_code",
                table: "leads");
        }
    }
}
