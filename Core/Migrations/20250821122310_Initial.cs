using System;
using System.Collections.Generic;
using BRB.Core.Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "food_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<MultiLanguageField>(type: "jsonb", nullable: false),
                    cover_url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_food_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<MultiLanguageField>(type: "jsonb", nullable: false),
                    time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    group_name = table.Column<MultiLanguageField>(type: "jsonb", nullable: true),
                    is_system_defined = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purposes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    group_name = table.Column<MultiLanguageField>(type: "jsonb", nullable: true),
                    is_system_defined = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purposes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sign_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    device_id = table.Column<long>(type: "bigint", nullable: false),
                    sign_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sign_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    r_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    r_token_expire_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    roles = table.Column<List<string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "foods",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<MultiLanguageField>(type: "jsonb", nullable: false),
                    cover_url = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_foods", x => x.id);
                    table.ForeignKey(
                        name: "fk_foods_food_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "food_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_foods_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    moment_id = table.Column<long>(type: "bigint", nullable: false),
                    before = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminders_moments_moment_id",
                        column: x => x.moment_id,
                        principalTable: "moments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reminders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_extras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    height = table.Column<double>(type: "double precision", nullable: false),
                    bmi = table.Column<double>(type: "double precision", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    photo = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_extras", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_extras_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_norm_by_menus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    menu = table.Column<int>(type: "integer", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_norm_by_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_norm_by_menus_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_norms_general",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_norms_general", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_norms_general_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_menus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    menu = table.Column<int>(type: "integer", nullable: false),
                    food_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_daily_menus_foods_food_id",
                        column: x => x.food_id,
                        principalTable: "foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_daily_menus_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "food_metrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    food_id = table.Column<long>(type: "bigint", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_food_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_food_metrics_foods_food_id",
                        column: x => x.food_id,
                        principalTable: "foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "food_user_extra",
                columns: table => new
                {
                    favourite_foods_id = table.Column<long>(type: "bigint", nullable: false),
                    user_extra_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_food_user_extra", x => new { x.favourite_foods_id, x.user_extra_id });
                    table.ForeignKey(
                        name: "fk_food_user_extra_foods_favourite_foods_id",
                        column: x => x.favourite_foods_id,
                        principalTable: "foods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_food_user_extra_user_extras_user_extra_id",
                        column: x => x.user_extra_id,
                        principalTable: "user_extras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purpose_user_extra",
                columns: table => new
                {
                    purposes_id = table.Column<long>(type: "bigint", nullable: false),
                    user_extra_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purpose_user_extra", x => new { x.purposes_id, x.user_extra_id });
                    table.ForeignKey(
                        name: "fk_purpose_user_extra_purposes_purposes_id",
                        column: x => x.purposes_id,
                        principalTable: "purposes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purpose_user_extra_user_extras_user_extra_id",
                        column: x => x.user_extra_id,
                        principalTable: "user_extras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_menus_food_id",
                table: "daily_menus",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_menus_user_id",
                table: "daily_menus",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_key",
                table: "devices",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "ix_devices_user_id",
                table: "devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_food_metrics_food_id",
                table: "food_metrics",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ix_food_user_extra_user_extra_id",
                table: "food_user_extra",
                column: "user_extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_foods_category_id",
                table: "foods",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_foods_user_id",
                table: "foods",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_moments_id",
                table: "moments",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_moments_time",
                table: "moments",
                column: "time");

            migrationBuilder.CreateIndex(
                name: "ix_purpose_user_extra_user_extra_id",
                table: "purpose_user_extra",
                column: "user_extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_purposes_id",
                table: "purposes",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reminders_moment_id",
                table: "reminders",
                column: "moment_id");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_user_id",
                table: "reminders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sign_logs_device_id",
                table: "sign_logs",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_sign_logs_sign_at",
                table: "sign_logs",
                column: "sign_at");

            migrationBuilder.CreateIndex(
                name: "ix_sign_logs_user_id",
                table: "sign_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_extras_name",
                table: "user_extras",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_user_extras_user_id",
                table: "user_extras",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_norm_by_menus_user_id",
                table: "user_norm_by_menus",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_norms_general_date",
                table: "user_norms_general",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_user_norms_general_user_id",
                table: "user_norms_general",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_menus");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "food_metrics");

            migrationBuilder.DropTable(
                name: "food_user_extra");

            migrationBuilder.DropTable(
                name: "purpose_user_extra");

            migrationBuilder.DropTable(
                name: "reminders");

            migrationBuilder.DropTable(
                name: "sign_logs");

            migrationBuilder.DropTable(
                name: "user_norm_by_menus");

            migrationBuilder.DropTable(
                name: "user_norms_general");

            migrationBuilder.DropTable(
                name: "foods");

            migrationBuilder.DropTable(
                name: "purposes");

            migrationBuilder.DropTable(
                name: "user_extras");

            migrationBuilder.DropTable(
                name: "moments");

            migrationBuilder.DropTable(
                name: "food_categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
