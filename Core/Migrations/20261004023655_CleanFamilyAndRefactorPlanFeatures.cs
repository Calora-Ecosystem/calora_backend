using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class CleanFamilyAndRefactorPlanFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_codes");

            // user_dailies."Discriminator" (EF fantom diff) allaqachon o'chirilgan.

            migrationBuilder.DropColumn(
                name: "is_family",
                table: "plan_extras");

            migrationBuilder.RenameColumn(
                name: "limit",
                table: "plan_features",
                newName: "value");

            migrationBuilder.AlterColumn<string>(
                name: "feature_key",
                table: "plan_features",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "plan_features",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "value",
                table: "plan_features");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "user_dailies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "feature_key",
                table: "plan_features",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "limit",
                table: "plan_features",
                type: "text",
                nullable: false,
                defaultValue: "");

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
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    redeemed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    expire_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    months = table.Column<int>(type: "integer", nullable: false),
                    redeemed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
    }
}
