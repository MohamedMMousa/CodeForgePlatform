using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeForge.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTracksCohortsCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded raw SQL: the live DB's index names for these two may differ from
            // what this regenerated model assumes (see docs/ARCHITECTURE.md §3 — the
            // Phase 0 InitialCreate snapshot fix reused an already-applied migration id).
            // IF EXISTS makes this safe regardless of the actual name; a stray
            // differently-named index left behind is harmless at this scale.
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_enrollments_source_request_id\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_enrollments_student_id_course_id\";");

            migrationBuilder.AddColumn<Guid>(
                name: "course_id",
                table: "leads",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "enrollments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by",
                table: "enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cohort_id",
                table: "enrollments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "course_id",
                table: "enrollment_requests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "coupon_code",
                table: "enrollment_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "coupon_id",
                table: "enrollment_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "enrollment_requests",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "final_price",
                table: "enrollment_requests",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "original_price",
                table: "enrollment_requests",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "track_id",
                table: "enrollment_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cohorts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enrollment_cutoff_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    grace_period_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cohorts", x => x.id);
                    table.ForeignKey(
                        name: "FK_cohorts_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.id);
                    table.ForeignKey(
                        name: "FK_coupons_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0.00m),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "EGP"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tracks_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enrollment_request_cohorts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    enrollment_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cohort_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollment_request_cohorts", x => x.id);
                    table.ForeignKey(
                        name: "FK_enrollment_request_cohorts_cohorts_cohort_id",
                        column: x => x.cohort_id,
                        principalTable: "cohorts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollment_request_cohorts_enrollment_requests_enrollment_r~",
                        column: x => x.enrollment_request_id,
                        principalTable: "enrollment_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "track_courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_courses_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_track_courses_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leads_course_id",
                table: "leads",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_cancelled_by",
                table: "enrollments",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_cohort_id",
                table: "enrollments",
                column: "cohort_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_source_request_id",
                table: "enrollments",
                column: "source_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_student_id_cohort_id",
                table: "enrollments",
                columns: new[] { "student_id", "cohort_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_requests_coupon_id",
                table: "enrollment_requests",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_requests_track_id",
                table: "enrollment_requests",
                column: "track_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_enrollment_requests_course_xor_track",
                table: "enrollment_requests",
                sql: "(course_id IS NOT NULL AND track_id IS NULL) OR (course_id IS NULL AND track_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_cohorts_course_id",
                table: "cohorts",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_cohorts_status",
                table: "cohorts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_created_by",
                table: "coupons",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_request_cohorts_cohort_id",
                table: "enrollment_request_cohorts",
                column: "cohort_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_request_cohorts_enrollment_request_id_cohort_id",
                table: "enrollment_request_cohorts",
                columns: new[] { "enrollment_request_id", "cohort_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_track_courses_course_id",
                table: "track_courses",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_courses_track_id_course_id",
                table: "track_courses",
                columns: new[] { "track_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tracks_created_by",
                table: "tracks",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tracks_slug",
                table: "tracks",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tracks_status",
                table: "tracks",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_enrollment_requests_coupons_coupon_id",
                table: "enrollment_requests",
                column: "coupon_id",
                principalTable: "coupons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_enrollment_requests_tracks_track_id",
                table: "enrollment_requests",
                column: "track_id",
                principalTable: "tracks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_enrollments_cohorts_cohort_id",
                table: "enrollments",
                column: "cohort_id",
                principalTable: "cohorts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_enrollments_users_cancelled_by",
                table: "enrollments",
                column: "cancelled_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_courses_course_id",
                table: "leads",
                column: "course_id",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_enrollment_requests_coupons_coupon_id",
                table: "enrollment_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_enrollment_requests_tracks_track_id",
                table: "enrollment_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_enrollments_cohorts_cohort_id",
                table: "enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_enrollments_users_cancelled_by",
                table: "enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_courses_course_id",
                table: "leads");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "enrollment_request_cohorts");

            migrationBuilder.DropTable(
                name: "track_courses");

            migrationBuilder.DropTable(
                name: "cohorts");

            migrationBuilder.DropTable(
                name: "tracks");

            migrationBuilder.DropIndex(
                name: "IX_leads_course_id",
                table: "leads");

            migrationBuilder.DropIndex(
                name: "IX_enrollments_cancelled_by",
                table: "enrollments");

            migrationBuilder.DropIndex(
                name: "IX_enrollments_cohort_id",
                table: "enrollments");

            migrationBuilder.DropIndex(
                name: "IX_enrollments_source_request_id",
                table: "enrollments");

            migrationBuilder.DropIndex(
                name: "IX_enrollments_student_id_cohort_id",
                table: "enrollments");

            migrationBuilder.DropIndex(
                name: "IX_enrollment_requests_coupon_id",
                table: "enrollment_requests");

            migrationBuilder.DropIndex(
                name: "IX_enrollment_requests_track_id",
                table: "enrollment_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_enrollment_requests_course_xor_track",
                table: "enrollment_requests");

            migrationBuilder.DropColumn(
                name: "course_id",
                table: "leads");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "cancelled_by",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "cohort_id",
                table: "enrollments");

            migrationBuilder.DropColumn(
                name: "coupon_code",
                table: "enrollment_requests");

            migrationBuilder.DropColumn(
                name: "coupon_id",
                table: "enrollment_requests");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "enrollment_requests");

            migrationBuilder.DropColumn(
                name: "final_price",
                table: "enrollment_requests");

            migrationBuilder.DropColumn(
                name: "original_price",
                table: "enrollment_requests");

            migrationBuilder.DropColumn(
                name: "track_id",
                table: "enrollment_requests");

            migrationBuilder.AlterColumn<Guid>(
                name: "course_id",
                table: "enrollment_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_source_request_id",
                table: "enrollments",
                column: "source_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_student_id_course_id",
                table: "enrollments",
                columns: new[] { "student_id", "course_id" },
                unique: true);
        }
    }
}
