using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeForge.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEnrollmentStatusCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // chk_enrollment_status predates EF's model (hand-authored in the original
            // schema.sql, never expressed via fluent config) and only allowed
            // 'active'/'expired'. Phase 1 added 'cancelled'/'refunded' to
            // EnrollmentStatuses — widen the constraint to match.
            migrationBuilder.Sql("ALTER TABLE enrollments DROP CONSTRAINT IF EXISTS chk_enrollment_status;");
            migrationBuilder.Sql(
                "ALTER TABLE enrollments ADD CONSTRAINT chk_enrollment_status " +
                "CHECK (status IN ('active', 'expired', 'cancelled', 'refunded'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE enrollments DROP CONSTRAINT IF EXISTS chk_enrollment_status;");
            migrationBuilder.Sql(
                "ALTER TABLE enrollments ADD CONSTRAINT chk_enrollment_status " +
                "CHECK (status IN ('active', 'expired'));");
        }
    }
}
