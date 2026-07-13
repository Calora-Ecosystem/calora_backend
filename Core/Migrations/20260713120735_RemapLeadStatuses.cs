using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <summary>
    /// Re-maps lead status codes after the pipeline was reduced to six stages
    /// (New, FollowUp=Qayta aloqa, Interested=O'ylab ko'radi, PaymentInProgress=To'lov jarayonda,
    /// Won, Lost). The removed Assigned/Contacted stages are folded into New / FollowUp.
    /// One CASE over the original value → no double-remap. Runs once via the migration history.
    /// </summary>
    public partial class RemapLeadStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE leads SET status = CASE status
                    WHEN 2 THEN 1   -- Assigned   → New (1)
                    WHEN 3 THEN 2   -- Contacted  → FollowUp / Qayta aloqa (2)
                    WHEN 5 THEN 2   -- FollowUp   → FollowUp / Qayta aloqa (2)
                    WHEN 4 THEN 3   -- Interested → Interested / O'ylab ko'radi (3)
                    WHEN 6 THEN 5   -- Won        → Won / Sotuv (5)
                    WHEN 7 THEN 6   -- Lost       → Lost / Yo'qotilgan (6)
                    ELSE status     -- New (1) unchanged
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old Assigned/Contacted stages were merged into New/FollowUp, so the
            // mapping is not losslessly reversible. Best-effort restore of the numbering.
            migrationBuilder.Sql("""
                UPDATE leads SET status = CASE status
                    WHEN 6 THEN 7   -- Lost → 7
                    WHEN 5 THEN 6   -- Won  → 6
                    WHEN 3 THEN 4   -- Interested → 4
                    WHEN 2 THEN 5   -- FollowUp   → 5
                    ELSE status
                END;
                """);
        }
    }
}
