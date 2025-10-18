using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableNorms2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_norms_users_user_id",
                table: "user_norms");

            migrationBuilder.RenameIndex(
                name: "ix_user_norms_user_id",
                table: "user_norms",
                newName: "IX_user_norms_user_id");

            // migrationBuilder.CreateSequence(
            //     name: "UserNormGeneralSequence");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_norms",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('\"UserNormGeneralSequence\"')",
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "user_dailies",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_dailies",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('\"UserNormGeneralSequence\"')",
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "metric",
                table: "user_dailies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "user_dailies",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "value",
                table: "user_dailies",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_user_dailies_user_id",
                table: "user_dailies",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_dailies_users_user_id",
                table: "user_dailies",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_norms_users_user_id",
                table: "user_norms",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_dailies_users_user_id",
                table: "user_dailies");

            migrationBuilder.DropForeignKey(
                name: "FK_user_norms_users_user_id",
                table: "user_norms");

            migrationBuilder.DropIndex(
                name: "IX_user_dailies_user_id",
                table: "user_dailies");

            migrationBuilder.DropColumn(
                name: "metric",
                table: "user_dailies");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "user_dailies");

            migrationBuilder.DropColumn(
                name: "value",
                table: "user_dailies");

            // migrationBuilder.DropSequence(
            //     name: "UserNormGeneralSequence");

            migrationBuilder.RenameIndex(
                name: "IX_user_norms_user_id",
                table: "user_norms",
                newName: "ix_user_norms_user_id");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_norms",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValueSql: "nextval('\"UserNormGeneralSequence\"')")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "user_dailies",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_dailies",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValueSql: "nextval('\"UserNormGeneralSequence\"')");

            migrationBuilder.AddForeignKey(
                name: "fk_user_norms_users_user_id",
                table: "user_norms",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
