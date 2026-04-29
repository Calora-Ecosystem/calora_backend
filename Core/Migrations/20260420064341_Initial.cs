using System;
using System.Collections.Generic;
using BRB.Core.Common.Models;
using Core.Entities.Course;
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
            migrationBuilder.CreateSequence(
                name: "UserNormGeneralSequence");

            migrationBuilder.CreateTable(
                name: "computations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    computation_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_computations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coupon_usages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    coupon_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupon_usages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    allowed_user_ids = table.Column<List<long>>(type: "jsonb", nullable: true),
                    usages = table.Column<int>(type: "integer", nullable: false),
                    one_time = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    expire_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "course_item_states",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_item_states", x => x.id);
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
                    info = table.Column<MultiLanguageField>(type: "jsonb", nullable: true),
                    title = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 100, nullable: false),
                    description = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 500, nullable: false),
                    assets = table.Column<Asset[]>(type: "jsonb", nullable: false),
                    order = table.Column<decimal>(type: "numeric(10,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                });

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
                name: "plan_extras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    fee = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    duration_in_months = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_extras", x => x.id);
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
                name: "reminder_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    menu = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminder_messages", x => x.id);
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
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    password = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    r_token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    r_token_expire_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    roles = table.Column<List<string>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workout_computation_indices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    total_duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    total_counts = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_computation_indices", x => x.id);
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
                    assets = table.Column<Asset[]>(type: "jsonb", nullable: false),
                    order = table.Column<decimal>(type: "numeric(10,3)", nullable: false)
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
                    course_id = table.Column<long>(type: "bigint", nullable: false),
                    has_rest = table.Column<bool>(type: "boolean", nullable: false),
                    title = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 100, nullable: false),
                    description = table.Column<MultiLanguageField>(type: "jsonb", maxLength: 500, nullable: false),
                    assets = table.Column<Asset[]>(type: "jsonb", nullable: false),
                    order = table.Column<decimal>(type: "numeric(10,3)", nullable: false)
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
                name: "plan_features",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_id = table.Column<long>(type: "bigint", nullable: false),
                    feature_key = table.Column<string>(type: "text", nullable: false),
                    limit = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_features_plan_extras_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plan_extras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    has_read = table.Column<bool>(type: "boolean", nullable: false),
                    discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    image = table.Column<string>(type: "text", nullable: true),
                    meta = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    enqueued_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    scheduled = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    failure_count = table.Column<int>(type: "integer", nullable: true),
                    success_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    coupon_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalTable: "coupons",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    menu = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    subscription_plan = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_dailies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_dailies", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_dailies_users_user_id",
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
                    entry_weight = table.Column<double>(type: "double precision", nullable: false),
                    height = table.Column<double>(type: "double precision", nullable: false),
                    bmi = table.Column<double>(type: "double precision", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    photo = table.Column<string>(type: "text", nullable: true),
                    physical_activity = table.Column<int>(type: "integer", nullable: true),
                    activity_level = table.Column<int>(type: "integer", nullable: false),
                    language = table.Column<int>(type: "integer", nullable: false),
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
                name: "user_norms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"UserNormGeneralSequence\"')"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_norms", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_norms_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_step_stats",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    sum = table.Column<double>(type: "double precision", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "fk_user_step_stats_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
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
                    assets = table.Column<Asset[]>(type: "jsonb", nullable: false),
                    order = table.Column<decimal>(type: "numeric(10,3)", nullable: false)
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
                name: "daily_menus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    menu = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
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
                name: "click_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    state = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    delivery = table.Column<long>(type: "bigint", nullable: false),
                    tax = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    invoice_id = table.Column<string>(type: "text", nullable: true),
                    payment_id = table.Column<string>(type: "text", nullable: true),
                    card_token = table.Column<string>(type: "text", nullable: true),
                    token = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_click_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_click_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payme_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    external_created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payme_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_payme_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    plan_extra_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subscription_orders_plan_extras_plan_extra_id",
                        column: x => x.plan_extra_id,
                        principalTable: "plan_extras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "ix_click_transactions_order_id",
                table: "click_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_computations_entity_id",
                table: "computations",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_usages_coupon_id_user_id_order_id",
                table: "coupon_usages",
                columns: new[] { "coupon_id", "user_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_course_item_states_user_id_entity_id_type",
                table: "course_item_states",
                columns: new[] { "user_id", "entity_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_courses_order",
                table: "courses",
                column: "order");

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
                name: "ix_exercise_metrics_exercise_id",
                table: "exercise_metrics",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_order",
                table: "exercises",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_workout_id",
                table: "exercises",
                column: "workout_id");

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
                name: "ix_lessons_course_id",
                table: "lessons",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_order",
                table: "lessons",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_coupon_id",
                table: "orders",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_orders_type",
                table: "orders",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payme_transactions_external_id",
                table: "payme_transactions",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payme_transactions_order_id",
                table: "payme_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_extras_plan",
                table: "plan_extras",
                column: "plan");

            migrationBuilder.CreateIndex(
                name: "ix_plan_features_plan_id_feature_key",
                table: "plan_features",
                columns: new[] { "plan_id", "feature_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purposes_id",
                table: "purposes",
                column: "id",
                unique: true);

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
                name: "ix_subscription_orders_order_id",
                table: "subscription_orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_orders_plan_extra_id",
                table: "subscription_orders",
                column: "plan_extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_subscription_plan",
                table: "subscriptions",
                column: "subscription_plan");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_user_id",
                table: "subscriptions",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_dailies_date",
                table: "user_dailies",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_user_dailies_user_id_date_metric",
                table: "user_dailies",
                columns: new[] { "user_id", "date", "metric" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_extras_name",
                table: "user_extras",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_user_extras_user_id",
                table: "user_extras",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_norm_by_menus_user_id",
                table: "user_norm_by_menus",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_norms_user_id",
                table: "user_norms",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_step_stats_user_id",
                table: "user_step_stats",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_versions_key",
                table: "versions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workout_computation_indices_entity_id_level",
                table: "workout_computation_indices",
                columns: new[] { "entity_id", "level" });

            migrationBuilder.CreateIndex(
                name: "ix_workouts_course_id",
                table: "workouts",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_workouts_order",
                table: "workouts",
                column: "order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "click_transactions");

            migrationBuilder.DropTable(
                name: "computations");

            migrationBuilder.DropTable(
                name: "coupon_usages");

            migrationBuilder.DropTable(
                name: "course_item_states");

            migrationBuilder.DropTable(
                name: "daily_menus");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "exercise_metrics");

            migrationBuilder.DropTable(
                name: "food_metrics");

            migrationBuilder.DropTable(
                name: "food_user_extra");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "payme_transactions");

            migrationBuilder.DropTable(
                name: "plan_features");

            migrationBuilder.DropTable(
                name: "purposes");

            migrationBuilder.DropTable(
                name: "reminder_messages");

            migrationBuilder.DropTable(
                name: "reminders");

            migrationBuilder.DropTable(
                name: "sign_logs");

            migrationBuilder.DropTable(
                name: "subscription_orders");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "user_dailies");

            migrationBuilder.DropTable(
                name: "user_norm_by_menus");

            migrationBuilder.DropTable(
                name: "user_norms");

            migrationBuilder.DropTable(
                name: "user_step_stats");

            migrationBuilder.DropTable(
                name: "versions");

            migrationBuilder.DropTable(
                name: "workout_computation_indices");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "foods");

            migrationBuilder.DropTable(
                name: "user_extras");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "plan_extras");

            migrationBuilder.DropTable(
                name: "workouts");

            migrationBuilder.DropTable(
                name: "food_categories");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropSequence(
                name: "UserNormGeneralSequence");
        }
    }
}
