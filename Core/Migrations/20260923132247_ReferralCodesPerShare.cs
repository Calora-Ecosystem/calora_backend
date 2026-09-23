using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class ReferralCodesPerShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "referrals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "referral_codes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referral_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_referral_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_referral_codes_code",
                table: "referral_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referral_codes_user_id_created_at",
                table: "referral_codes",
                columns: new[] { "user_id", "created_at" });

            // Mavjud (users.referral_code) kodlar ham amal qilaverishi uchun ko'chiriladi.
            migrationBuilder.Sql(@"
insert into referral_codes (user_id, code, created_at, updated_at)
select id, referral_code, localtimestamp, localtimestamp
from users
where referral_code is not null
on conflict (code) do nothing;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referral_codes");

            migrationBuilder.DropColumn(
                name: "code",
                table: "referrals");
        }
    }
}
