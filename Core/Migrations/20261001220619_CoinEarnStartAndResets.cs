using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class CoinEarnStartAndResets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.

            migrationBuilder.CreateTable(
                name: "coin_resets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    earn_start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    users_affected = table.Column<int>(type: "integer", nullable: false),
                    balance_removed = table.Column<long>(type: "bigint", nullable: false),
                    earned_removed = table.Column<long>(type: "bigint", nullable: false),
                    transactions_removed = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coin_resets", x => x.id);
                    table.ForeignKey(
                        name: "fk_coin_resets_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "coin_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    earn_start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_by_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coin_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_coin_settings_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coin_resets_created_by_id",
                table: "coin_resets",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_coin_settings_updated_by_id",
                table: "coin_settings",
                column: "updated_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coin_resets");

            migrationBuilder.DropTable(
                name: "coin_settings");
        }
    }
}
