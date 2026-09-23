using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class MarketOnlyPremiumTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Do'kon faqat Premium tariflardan iborat: 150→7, 300→30, 600→75, 900→120 kun.
            // category 1 = Tariff, reward_type 1 = PremiumDays. Bonus/vaucher mahsulotlari o'chiriladi
            // (xarid tarixi saqlanishi uchun qator o'chirilmaydi, faqat is_active = false).
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.
            migrationBuilder.Sql(@"
update market_items set is_active = false, updated_at = localtimestamp
where category <> 1 or reward_type <> 1;

update market_items
set price_coins = 150, reward_value = 7, is_popular = false, is_active = true, sort_order = 10,
    subtitle = 'mi_premium_7_sub', updated_at = localtimestamp
where title = 'mi_premium_7';

update market_items
set price_coins = 300, reward_value = 30, is_popular = true, is_active = true, sort_order = 20,
    subtitle = 'mi_premium_30_sub', updated_at = localtimestamp
where title = 'mi_premium_30';

insert into market_items (title, subtitle, price_coins, category, reward_type, reward_value, is_popular, is_active, sort_order, created_at, updated_at)
select v.title, v.subtitle, v.price, 1, 1, v.days, false, true, v.sort, localtimestamp, localtimestamp
from (values
    ('mi_premium_7',   'mi_premium_7_sub',   150, 7,   10),
    ('mi_premium_30',  'mi_premium_30_sub',  300, 30,  20),
    ('mi_premium_75',  'mi_premium_75_sub',  600, 75,  30),
    ('mi_premium_120', 'mi_premium_120_sub', 900, 120, 40)
) as v(title, subtitle, price, days, sort)
where not exists (select 1 from market_items m where m.title = v.title);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
update market_items set is_active = false where title in ('mi_premium_75', 'mi_premium_120');
update market_items set price_coins = 90 where title = 'mi_premium_7';
update market_items set is_active = true where title = 'mi_ai_pack';
");
        }
    }
}
