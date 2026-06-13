using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "app_open_count",
                table: "leads",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "leads",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "food_tracked_count",
                table: "leads",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_contacted_at",
                table: "leads",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lost_at",
                table: "leads",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lost_reason",
                table: "leads",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_follow_up_at",
                table: "leads",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "operator_id",
                table: "leads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_provider",
                table: "leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "score",
                table: "leads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "leads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "temperature",
                table: "leads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "water_tracked_count",
                table: "leads",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "won_amount",
                table: "leads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "won_at",
                table: "leads",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "workout_started_count",
                table: "leads",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "follow_ups",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lead_id = table.Column<long>(type: "bigint", nullable: false),
                    operator_id = table.Column<long>(type: "bigint", nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    done_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    escalated = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_follow_ups", x => x.id);
                    table.ForeignKey(
                        name: "fk_follow_ups_leads_lead_id",
                        column: x => x.lead_id,
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_follow_ups_users_operator_id",
                        column: x => x.operator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lead_activities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lead_id = table.Column<long>(type: "bigint", nullable: false),
                    actor_id = table.Column<long>(type: "bigint", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_lead_activities_leads_lead_id",
                        column: x => x.lead_id,
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lead_activities_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_leads_next_follow_up_at",
                table: "leads",
                column: "next_follow_up_at");

            migrationBuilder.CreateIndex(
                name: "ix_leads_operator_id",
                table: "leads",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_score",
                table: "leads",
                column: "score");

            migrationBuilder.CreateIndex(
                name: "ix_leads_status",
                table: "leads",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_due_at",
                table: "follow_ups",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_is_done",
                table: "follow_ups",
                column: "is_done");

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_lead_id",
                table: "follow_ups",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_operator_id",
                table: "follow_ups",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_activities_actor_id",
                table: "lead_activities",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_activities_lead_id",
                table: "lead_activities",
                column: "lead_id");

            migrationBuilder.AddForeignKey(
                name: "fk_leads_users_operator_id",
                table: "leads",
                column: "operator_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_leads_users_operator_id",
                table: "leads");

            migrationBuilder.DropTable(
                name: "follow_ups");

            migrationBuilder.DropTable(
                name: "lead_activities");

            migrationBuilder.DropIndex(
                name: "ix_leads_next_follow_up_at",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "ix_leads_operator_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "ix_leads_score",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "ix_leads_status",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "app_open_count",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "food_tracked_count",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "last_contacted_at",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "lost_at",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "lost_reason",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "next_follow_up_at",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "operator_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "payment_provider",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "score",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "status",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "temperature",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "water_tracked_count",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "won_amount",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "won_at",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "workout_started_count",
                table: "leads");
        }
    }
}
