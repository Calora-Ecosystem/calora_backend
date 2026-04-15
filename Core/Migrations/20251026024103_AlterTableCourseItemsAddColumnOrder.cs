using BRB.Core.Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableCourseItemsAddColumnOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_dailies_users_user_id",
                table: "user_dailies");

            migrationBuilder.DropForeignKey(
                name: "FK_user_norms_users_user_id",
                table: "user_norms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_norms",
                table: "user_norms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_dailies",
                table: "user_dailies");

            migrationBuilder.RenameIndex(
                name: "IX_user_norms_user_id",
                table: "user_norms",
                newName: "ix_user_norms_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_dailies_user_id",
                table: "user_dailies",
                newName: "ix_user_dailies_user_id");

            migrationBuilder.AddColumn<string[]>(
                name: "assets",
                table: "workouts",
                type: "jsonb",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<MultiLanguageField>(
                name: "description",
                table: "workouts",
                type: "jsonb",
                maxLength: 500,
                nullable: false);

            migrationBuilder.AddColumn<decimal>(
                name: "order",
                table: "workouts",
                type: "numeric(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "type",
                table: "workouts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_dailies",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValueSql: "nextval('\"UserNormGeneralSequence\"')")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<decimal>(
                name: "order",
                table: "lessons",
                type: "numeric(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "order",
                table: "exercises",
                type: "numeric(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "order",
                table: "courses",
                type: "numeric(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_norms",
                table: "user_norms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_dailies",
                table: "user_dailies",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_workouts_order",
                table: "workouts",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_order",
                table: "lessons",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_order",
                table: "exercises",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "ix_courses_order",
                table: "courses",
                column: "order");

            migrationBuilder.AddForeignKey(
                name: "fk_user_dailies_users_user_id",
                table: "user_dailies",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_norms_users_user_id",
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
                name: "fk_user_dailies_users_user_id",
                table: "user_dailies");

            migrationBuilder.DropForeignKey(
                name: "fk_user_norms_users_user_id",
                table: "user_norms");

            migrationBuilder.DropIndex(
                name: "ix_workouts_order",
                table: "workouts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_norms",
                table: "user_norms");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_dailies",
                table: "user_dailies");

            migrationBuilder.DropIndex(
                name: "ix_lessons_order",
                table: "lessons");

            migrationBuilder.DropIndex(
                name: "ix_exercises_order",
                table: "exercises");

            migrationBuilder.DropIndex(
                name: "ix_courses_order",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "assets",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "description",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "order",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "type",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "order",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "order",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "order",
                table: "courses");

            migrationBuilder.RenameIndex(
                name: "ix_user_norms_user_id",
                table: "user_norms",
                newName: "IX_user_norms_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_dailies_user_id",
                table: "user_dailies",
                newName: "IX_user_dailies_user_id");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_dailies",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('\"UserNormGeneralSequence\"')",
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_norms",
                table: "user_norms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_dailies",
                table: "user_dailies",
                column: "id");

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
    }
}
