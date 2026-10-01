using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class FamilyPlanCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.

            migrationBuilder.AddColumn<bool>(
                name: "is_family",
                table: "plan_extras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "family_codes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    months = table.Column<int>(type: "integer", nullable: false),
                    expire_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    redeemed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    redeemed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_family_codes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_codes_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_family_codes_users_redeemed_by_id",
                        column: x => x.redeemed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_family_codes_code",
                table: "family_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_codes_order_id",
                table: "family_codes",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_codes_owner_id_created_at",
                table: "family_codes",
                columns: new[] { "owner_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_family_codes_redeemed_by_id",
                table: "family_codes",
                column: "redeemed_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_codes");

            // Ustun o'chgach oilaviy paket oddiy oylik bo'lib qolmasin (orderlar unga bog'langan — o'chirilmaydi).
            migrationBuilder.Sql("UPDATE plan_extras SET is_active = FALSE, is_popular = FALSE WHERE is_family;");

            migrationBuilder.DropColumn(
                name: "is_family",
                table: "plan_extras");
        }
    }
}
