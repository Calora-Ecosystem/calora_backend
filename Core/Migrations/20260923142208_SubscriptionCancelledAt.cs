using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionCancelledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "subscriptions",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "subscriptions");
        }
    }
}
