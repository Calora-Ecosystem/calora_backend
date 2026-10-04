using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class ConvertFeatureKeyToEnumPlanFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE plan_features 
                ALTER COLUMN feature_key TYPE integer 
                USING (
                    CASE 
                        WHEN feature_key = 'ai_scans' THEN 1 
                        WHEN feature_key = 'family' THEN 2 
                        WHEN feature_key ~ '^[0-9]+$' THEN feature_key::integer
                        ELSE 1 
                    END
                );
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE user_feature_usages 
                ALTER COLUMN feature_key TYPE integer 
                USING (
                    CASE 
                        WHEN feature_key = 'ai_scans' THEN 1 
                        WHEN feature_key = 'family' THEN 2 
                        WHEN feature_key ~ '^[0-9]+$' THEN feature_key::integer
                        ELSE 1 
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE plan_features 
                ALTER COLUMN feature_key TYPE character varying(100) 
                USING (
                    CASE 
                        WHEN feature_key = 1 THEN 'ai_scans' 
                        WHEN feature_key = 2 THEN 'family' 
                        ELSE 'ai_scans' 
                    END
                );
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE user_feature_usages 
                ALTER COLUMN feature_key TYPE character varying(100) 
                USING (
                    CASE 
                        WHEN feature_key = 1 THEN 'ai_scans' 
                        WHEN feature_key = 2 THEN 'family' 
                        ELSE 'ai_scans' 
                    END
                );
            ");
        }
    }
}
