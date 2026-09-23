using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCoinsReferralStepGroupsAiQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eslatma: EF bu yerda user_dailies."Discriminator" ni o'chirishni ham generatsiya qiladi —
            // u ustun 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan, shuning uchun olib tashlandi.

            migrationBuilder.AddColumn<string>(
                name: "referral_code",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "bonus_days",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 1); // EnumSubscriptionSource.Payment — mavjud obunalar to'langan deb hisoblanadi

            migrationBuilder.AddColumn<long>(
                name: "referral_discount",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "coin_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ref_id = table.Column<long>(type: "bigint", nullable: true),
                    calora = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coin_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_coin_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coin_wallets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    total_earned = table.Column<long>(type: "bigint", nullable: false),
                    total_spent = table.Column<long>(type: "bigint", nullable: false),
                    calora_exchanged = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coin_wallets", x => x.id);
                    table.ForeignKey(
                        name: "fk_coin_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "market_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    price_coins = table.Column<long>(type: "bigint", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    reward_type = table.Column<int>(type: "integer", nullable: false),
                    reward_value = table.Column<long>(type: "bigint", nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "referral_premium_grants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    referrer_id = table.Column<long>(type: "bigint", nullable: false),
                    milestone = table.Column<int>(type: "integer", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referral_premium_grants", x => x.id);
                    table.ForeignKey(
                        name: "fk_referral_premium_grants_users_referrer_id",
                        column: x => x.referrer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "referrals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    referrer_id = table.Column<long>(type: "bigint", nullable: false),
                    referred_user_id = table.Column<long>(type: "bigint", nullable: false),
                    qualified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    referrer_reward = table.Column<long>(type: "bigint", nullable: false),
                    referred_reward = table.Column<long>(type: "bigint", nullable: false),
                    discount_used_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    discount_order_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referrals", x => x.id);
                    table.ForeignKey(
                        name: "fk_referrals_users_referred_user_id",
                        column: x => x.referred_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_referrals_users_referrer_id",
                        column: x => x.referrer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "step_groups",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_step_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_step_groups_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_ai_quotas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    used = table.Column<int>(type: "integer", nullable: false),
                    bonus_limit = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_ai_quotas", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_ai_quotas_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "market_purchases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    market_item_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price_coins = table.Column<long>(type: "bigint", nullable: false),
                    reward_type = table.Column<int>(type: "integer", nullable: false),
                    reward_value = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_purchases", x => x.id);
                    table.ForeignKey(
                        name: "fk_market_purchases_market_items_market_item_id",
                        column: x => x.market_item_id,
                        principalTable: "market_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_market_purchases_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "step_group_members",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_step_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_step_group_members_step_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "step_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_step_group_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_referral_code",
                table: "users",
                column: "referral_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_coin_transactions_type_created_at",
                table: "coin_transactions",
                columns: new[] { "type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_coin_transactions_user_id_created_at",
                table: "coin_transactions",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_coin_wallets_user_id",
                table: "coin_wallets",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_market_items_is_active_sort_order",
                table: "market_items",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_market_purchases_market_item_id",
                table: "market_purchases",
                column: "market_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_market_purchases_user_id_created_at",
                table: "market_purchases",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_referral_premium_grants_referrer_id_milestone",
                table: "referral_premium_grants",
                columns: new[] { "referrer_id", "milestone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referrals_referred_user_id",
                table: "referrals",
                column: "referred_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referrals_referrer_id_qualified_at",
                table: "referrals",
                columns: new[] { "referrer_id", "qualified_at" });

            migrationBuilder.CreateIndex(
                name: "ix_step_group_members_group_id_user_id",
                table: "step_group_members",
                columns: new[] { "group_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_step_group_members_user_id",
                table: "step_group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_step_groups_invite_code",
                table: "step_groups",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_step_groups_owner_id",
                table: "step_groups",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_ai_quotas_user_id",
                table: "user_ai_quotas",
                column: "user_id",
                unique: true);

            // Boshlang'ich marketplace katalogi (mobile lokalizatsiya kalitlari bilan).
            // category: 1 Tariff, 2 Voucher, 3 Boost; reward_type: 1 PremiumDays, 2 AiScans, 3 Coupon, 4 Voucher.
            // Chegirma va yetkazib berish vaucherlari admin qiymat berguncha o'chiq turadi.
            migrationBuilder.Sql(@"
insert into market_items (title, subtitle, price_coins, category, reward_type, reward_value, is_popular, is_active, sort_order, created_at, updated_at)
values
    ('mi_premium_7',      'mi_premium_7_sub',      90,  1, 1, 7,  false, true,  10, localtimestamp, localtimestamp),
    ('mi_premium_30',     'mi_premium_30_sub',     300, 1, 1, 30, true,  true,  20, localtimestamp, localtimestamp),
    ('mi_ai_pack',        'mi_ai_pack_sub',        50,  3, 2, 10, false, true,  30, localtimestamp, localtimestamp),
    ('mi_discount',       'mi_discount_sub',       150, 2, 3, 0,  false, false, 40, localtimestamp, localtimestamp),
    ('mi_free_delivery',  'mi_free_delivery_sub',  60,  2, 4, 0,  false, false, 50, localtimestamp, localtimestamp);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coin_transactions");

            migrationBuilder.DropTable(
                name: "coin_wallets");

            migrationBuilder.DropTable(
                name: "market_purchases");

            migrationBuilder.DropTable(
                name: "referral_premium_grants");

            migrationBuilder.DropTable(
                name: "referrals");

            migrationBuilder.DropTable(
                name: "step_group_members");

            migrationBuilder.DropTable(
                name: "user_ai_quotas");

            migrationBuilder.DropTable(
                name: "market_items");

            migrationBuilder.DropTable(
                name: "step_groups");

            migrationBuilder.DropIndex(
                name: "ix_users_referral_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "referral_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bonus_days",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "source",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "referral_discount",
                table: "orders");
        }
    }
}
