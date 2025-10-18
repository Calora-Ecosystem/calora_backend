using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableNorms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_user_norms",
                table: "user_norms");

            migrationBuilder.DropIndex(
                name: "ix_user_norms_date",
                table: "user_norms");

            migrationBuilder.DropColumn(
                name: "date",
                table: "user_norms");

            migrationBuilder.DropColumn(
                name: "discriminator",
                table: "user_norms");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_norms",
                table: "user_norms",
                column: "id");

            migrationBuilder.CreateTable(
                name: "user_dailies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_dailies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_dailies_date",
                table: "user_dailies",
                column: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_dailies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_norms",
                table: "user_norms");

            migrationBuilder.AddColumn<DateTime>(
                name: "date",
                table: "user_norms",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discriminator",
                table: "user_norms",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_norms",
                table: "user_norms",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_user_norms_date",
                table: "user_norms",
                column: "date");
        }
    }
}
