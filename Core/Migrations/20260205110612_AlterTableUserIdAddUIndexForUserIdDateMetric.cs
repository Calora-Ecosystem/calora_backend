using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableUserIdAddUIndexForUserIdDateMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_dailies_user_id",
                table: "user_dailies");
            
            migrationBuilder.CreateIndex(
                name: "ix_user_dailies_user_id_date_metric",
                table: "user_dailies",
                columns: new[] { "user_id", "date", "metric" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_dailies_user_id_date_metric",
                table: "user_dailies");
            
            migrationBuilder.CreateIndex(
                name: "ix_user_dailies_user_id",
                table: "user_dailies",
                column: "user_id");
        }
    }
}
