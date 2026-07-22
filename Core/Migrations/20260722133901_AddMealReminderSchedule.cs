using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMealReminderSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "reminder_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "time",
                table: "reminder_messages",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "meal_gate_menu",
                table: "notifications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "reminder_messages");

            migrationBuilder.DropColumn(
                name: "time",
                table: "reminder_messages");

            migrationBuilder.DropColumn(
                name: "meal_gate_menu",
                table: "notifications");
        }
    }
}
