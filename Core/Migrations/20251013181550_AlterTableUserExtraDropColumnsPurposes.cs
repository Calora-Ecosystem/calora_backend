using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableUserExtraDropColumnsPurposes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_histories");

            migrationBuilder.DropTable(
                name: "purpose_user_extra");

            migrationBuilder.DropIndex(
                name: "ix_user_extras_user_id",
                table: "user_extras");

            migrationBuilder.AddColumn<int>(
                name: "purpose",
                table: "user_extras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "course_item_states",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_item_states", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_extras_user_id",
                table: "user_extras",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_course_item_states_user_id_entity_id_type",
                table: "course_item_states",
                columns: new[] { "user_id", "entity_id", "type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_item_states");

            migrationBuilder.DropIndex(
                name: "ix_user_extras_user_id",
                table: "user_extras");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "user_extras");

            migrationBuilder.CreateTable(
                name: "course_histories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purpose_user_extra",
                columns: table => new
                {
                    purposes_id = table.Column<long>(type: "bigint", nullable: false),
                    user_extra_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purpose_user_extra", x => new { x.purposes_id, x.user_extra_id });
                    table.ForeignKey(
                        name: "fk_purpose_user_extra_purposes_purposes_id",
                        column: x => x.purposes_id,
                        principalTable: "purposes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purpose_user_extra_user_extras_user_extra_id",
                        column: x => x.user_extra_id,
                        principalTable: "user_extras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_extras_user_id",
                table: "user_extras",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_histories_user_id_entity_id_type",
                table: "course_histories",
                columns: new[] { "user_id", "entity_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_purpose_user_extra_user_extra_id",
                table: "purpose_user_extra",
                column: "user_extra_id");
        }
    }
}
