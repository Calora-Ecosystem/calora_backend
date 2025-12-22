using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPushNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "user_dailies");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "time",
                table: "reminders",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AddColumn<string>(
                name: "discriminator",
                table: "notifications",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "failure_count",
                table: "notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "meta",
                table: "notifications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled",
                table: "notifications",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_at",
                table: "notifications",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "success_count",
                table: "notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "tokens",
                table: "notifications",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discriminator",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "failure_count",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "image",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "meta",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "scheduled",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "success_count",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "tokens",
                table: "notifications");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "user_dailies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "time",
                table: "reminders",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");
        }
    }
}
