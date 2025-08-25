using System;
using BRB.Core.Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reminders_moments_moment_id",
                table: "reminders");

            migrationBuilder.DropTable(
                name: "moments");

            migrationBuilder.DropIndex(
                name: "ix_reminders_moment_id",
                table: "reminders");

            migrationBuilder.DropColumn(
                name: "moment_id",
                table: "reminders");

            migrationBuilder.RenameColumn(
                name: "before",
                table: "reminders",
                newName: "time");

            migrationBuilder.AddColumn<int>(
                name: "activity_level",
                table: "user_extras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "language",
                table: "user_extras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "time",
                table: "reminders",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AddColumn<int>(
                name: "menu",
                table: "reminders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "reminders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "course_histories",
                columns: table => new
                {
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 100, nullable: false),
                    description = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 500, nullable: false),
                    assets = table.Column<string[]>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    is_free = table.Column<bool>(type: "boolean", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    title = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 100, nullable: false),
                    description = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 500, nullable: false),
                    assets = table.Column<string[]>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.ForeignKey(
                        name: "fk_lessons_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<MultiLanguageField>(type: "jsonb", nullable: false),
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    has_rest = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_workouts_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workout_id = table.Column<long>(type: "bigint", nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    title = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 100, nullable: false),
                    description = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 500, nullable: false),
                    assets = table.Column<string[]>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercises_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_metrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exercise_id = table.Column<long>(type: "bigint", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_metrics_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_course_histories_user_id_entity_id_type",
                table: "course_histories",
                columns: new[] { "user_id", "entity_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_exercise_metrics_exercise_id",
                table: "exercise_metrics",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_workout_id",
                table: "exercises",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_course_id",
                table: "lessons",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_workouts_course_id",
                table: "workouts",
                column: "course_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_histories");

            migrationBuilder.DropTable(
                name: "exercise_metrics");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "workouts");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropColumn(
                name: "activity_level",
                table: "user_extras");

            migrationBuilder.DropColumn(
                name: "language",
                table: "user_extras");

            migrationBuilder.DropColumn(
                name: "menu",
                table: "reminders");

            migrationBuilder.DropColumn(
                name: "type",
                table: "reminders");

            migrationBuilder.RenameColumn(
                name: "time",
                table: "reminders",
                newName: "before");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "before",
                table: "reminders",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AddColumn<long>(
                name: "moment_id",
                table: "reminders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "moments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_name = table.Column<MultiLanguageField>(type: "jsonb", nullable: true),
                    is_system_defined = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<MultiLanguageField>(type: "jsonb", nullable: false),
                    time = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reminders_moment_id",
                table: "reminders",
                column: "moment_id");

            migrationBuilder.CreateIndex(
                name: "ix_moments_id",
                table: "moments",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_moments_time",
                table: "moments",
                column: "time");

            migrationBuilder.AddForeignKey(
                name: "fk_reminders_moments_moment_id",
                table: "reminders",
                column: "moment_id",
                principalTable: "moments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
