using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_has_read",
                table: "notifications",
                columns: new[] { "user_id", "has_read" });

            migrationBuilder.CreateIndex(
                name: "ix_push_notifications_enqueued_at_null",
                table: "notifications",
                columns: new[] { "scheduled", "created_at" },
                filter: "enqueued_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_push_notifications_user_id_sent_at",
                table: "notifications",
                columns: new[] { "user_id", "sent_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_user_id_has_read",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_push_notifications_enqueued_at_null",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_push_notifications_user_id_sent_at",
                table: "notifications");
        }
    }
}
