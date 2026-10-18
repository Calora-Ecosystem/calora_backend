using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class InviteCodesWithoutDash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_dailies."Discriminator" (EF fantom diff) 20260506093122_AddUserSoftDelete'da allaqachon o'chirilgan.

            // Taklif kodlari endi chiziqchasiz (CAL-XXXXX → CALXXXXX, CALORA-XXXXXX → CALORAXXXXXX) —
            // messenjerda kod bir bosishda to'liq belgilanadi. Mavjud kodlar ham shu ko'rinishga o'tadi;
            // chiziqcha bilan ulashilgan eski xabarlar ishlayveradi (kiritilgan kod CodeGenerator.Normalize'dan o'tadi).
            // Chiziqchasiz ko'rinishi boshqa kod bilan bir xil bo'lib qoladigan kod (unique) o'zgartirilmaydi.
            migrationBuilder.Sql(@"
with normalized as (
    select id, replace(invite_code, '-', '') as new_code,
           count(*) over (partition by replace(invite_code, '-', '')) as same
    from step_groups
)
update step_groups g
set invite_code = n.new_code
from normalized n
where g.id = n.id and g.invite_code <> n.new_code and n.same = 1;

with normalized as (
    select id, replace(code, '-', '') as new_code,
           count(*) over (partition by replace(code, '-', '')) as same
    from referral_codes
)
update referral_codes c
set code = n.new_code
from normalized n
where c.id = n.id and c.code <> n.new_code and n.same = 1;

update referrals
set code = replace(code, '-', '')
where code like '%-%';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
update step_groups
set invite_code = 'CAL-' || substring(invite_code from 4)
where invite_code like 'CAL%' and invite_code not like 'CAL-%';

update referral_codes
set code = 'CALORA-' || substring(code from 7)
where code like 'CALORA%' and code not like 'CALORA-%';

update referrals
set code = 'CALORA-' || substring(code from 7)
where code like 'CALORA%' and code not like 'CALORA-%';
");
        }
    }
}
