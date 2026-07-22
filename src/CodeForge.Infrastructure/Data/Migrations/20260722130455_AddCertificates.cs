using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeForge.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "completion_attendance_threshold",
                table: "courses",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cohort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    verification_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attendance_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    assessments_passed = table.Column<bool>(type: "boolean", nullable: false),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                    table.ForeignKey(
                        name: "FK_certificates_cohorts_cohort_id",
                        column: x => x.cohort_id,
                        principalTable: "cohorts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_certificates_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_certificates_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_certificates_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_certificates_users_revoked_by",
                        column: x => x.revoked_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_certificates_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "chk_course_attendance_threshold",
                table: "courses",
                sql: "completion_attendance_threshold IS NULL OR (completion_attendance_threshold BETWEEN 0 AND 100)");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_cohort_id",
                table: "certificates",
                column: "cohort_id");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_course_id",
                table: "certificates",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_enrollment_id",
                table: "certificates",
                column: "enrollment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificates_issued_by",
                table: "certificates",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_revoked_by",
                table: "certificates",
                column: "revoked_by");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_serial_number",
                table: "certificates",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificates_student_id",
                table: "certificates",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_verification_code",
                table: "certificates",
                column: "verification_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificates");

            migrationBuilder.DropCheckConstraint(
                name: "chk_course_attendance_threshold",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "completion_attendance_threshold",
                table: "courses");
        }
    }
}
