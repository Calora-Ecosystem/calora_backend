using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class UseTpcMappingForUserNorms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_norms_general_users_user_id",
                table: "user_norms_general");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_norms_general",
                table: "user_norms_general");

            migrationBuilder.DropIndex(
                name: "ix_user_norms_general_date",
                table: "user_norms_general");

            migrationBuilder.DropColumn(
                name: "date",
                table: "user_norms_general");

            migrationBuilder.DropColumn(
                name: "discriminator",
                table: "user_norms_general");

            migrationBuilder.RenameIndex(
                name: "ix_user_norms_general_user_id",
                table: "user_norms_general",
                newName: "IX_user_norms_general_user_id");

            migrationBuilder.CreateSequence(
                name: "UserNormGeneralSequence");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_norms_general",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('\"UserNormGeneralSequence\"')",
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_norms_general",
                table: "user_norms_general",
                column: "id");

            migrationBuilder.CreateTable(
                name: "user_dailies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"UserNormGeneralSequence\"')"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_dailies", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_dailies_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_dailies_date",
                table: "user_dailies",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_user_dailies_user_id",
                table: "user_dailies",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_norms_general_users_user_id",
                table: "user_norms_general",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_norms_general_users_user_id",
                table: "user_norms_general");

            migrationBuilder.DropTable(
                name: "user_dailies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_norms_general",
                table: "user_norms_general");

            migrationBuilder.DropSequence(
                name: "UserNormGeneralSequence");

            migrationBuilder.RenameIndex(
                name: "IX_user_norms_general_user_id",
                table: "user_norms_general",
                newName: "ix_user_norms_general_user_id");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_norms_general",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValueSql: "nextval('\"UserNormGeneralSequence\"')")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "date",
                table: "user_norms_general",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discriminator",
                table: "user_norms_general",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_norms_general",
                table: "user_norms_general",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_user_norms_general_date",
                table: "user_norms_general",
                column: "date");

            migrationBuilder.AddForeignKey(
                name: "fk_user_norms_general_users_user_id",
                table: "user_norms_general",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
