using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class CoinsFromStepsRemoveCalora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.

            migrationBuilder.DropColumn(
                name: "calora_exchanged",
                table: "coin_wallets");

            migrationBuilder.DropColumn(
                name: "calora",
                table: "coin_transactions");

            migrationBuilder.CreateIndex(
                name: "ix_coin_transactions_user_id_type_ref_id",
                table: "coin_transactions",
                columns: new[] { "user_id", "type", "ref_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_coin_transactions_user_id_type_ref_id",
                table: "coin_transactions");

            migrationBuilder.AddColumn<long>(
                name: "calora_exchanged",
                table: "coin_wallets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "calora",
                table: "coin_transactions",
                type: "bigint",
                nullable: true);
        }
    }
}
