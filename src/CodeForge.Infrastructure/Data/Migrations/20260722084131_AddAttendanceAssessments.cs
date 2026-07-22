using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeForge.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quizzes_courses_course_id",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "allow_retake",
                table: "quizzes");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "quizzes",
                newName: "module_id");

            migrationBuilder.RenameIndex(
                name: "IX_quizzes_course_id",
                table: "quizzes",
                newName: "IX_quizzes_module_id");

            migrationBuilder.AddColumn<bool>(
                name: "disable_copy_paste",
                table: "quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_practice",
                table: "quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_attempts",
                table: "quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "randomize_questions",
                table: "quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "quizzes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "attempt_number",
                table: "quiz_attempts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_practice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pass_score = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignments", x => x.id);
                    table.CheckConstraint("chk_assignment_pass_score", "pass_score IS NULL OR (pass_score BETWEEN 0 AND 100)");
                    table.ForeignKey(
                        name: "FK_assignments_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    marked_by = table.Column<Guid>(type: "uuid", nullable: false),
                    marked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_records_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendance_records_users_marked_by",
                        column: x => x.marked_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_records_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignment_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_late = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    auto_score = table.Column<int>(type: "integer", nullable: true),
                    auto_grading_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    manual_score = table.Column<int>(type: "integer", nullable: true),
                    manual_feedback = table.Column<string>(type: "text", nullable: true),
                    final_score = table.Column<int>(type: "integer", nullable: true),
                    graded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    graded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignment_submissions_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignment_submissions_users_graded_by",
                        column: x => x.graded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assignment_submissions_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignment_test_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input = table.Column<string>(type: "text", nullable: false),
                    expected_output = table.Column<string>(type: "text", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    points = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    order_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_test_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignment_test_cases_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignment_test_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    actual_output = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    execution_time_ms = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_test_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignment_test_results_assignment_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "assignment_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignment_test_results_assignment_test_cases_test_case_id",
                        column: x => x.test_case_id,
                        principalTable: "assignment_test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // chk_quiz_pass_score predates EF's model (hand-authored in the original
            // schema.sql, never expressed via fluent config until now — see
            // docs/DATABASE.md §4a) and already exists on the live DB; guard the add
            // so this migration is idempotent regardless of which environment it runs on.
            migrationBuilder.Sql("ALTER TABLE quizzes DROP CONSTRAINT IF EXISTS chk_quiz_pass_score;");
            migrationBuilder.Sql(
                "ALTER TABLE quizzes ADD CONSTRAINT chk_quiz_pass_score " +
                "CHECK (pass_score IS NULL OR (pass_score BETWEEN 0 AND 100));");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_submissions_assignment_id",
                table: "assignment_submissions",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_submissions_graded_by",
                table: "assignment_submissions",
                column: "graded_by");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_submissions_student_id",
                table: "assignment_submissions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_test_cases_assignment_id",
                table: "assignment_test_cases",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_test_results_submission_id",
                table: "assignment_test_results",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_test_results_test_case_id",
                table: "assignment_test_results",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_module_id",
                table: "assignments",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_marked_by",
                table: "attendance_records",
                column: "marked_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_session_id_student_id",
                table: "attendance_records",
                columns: new[] { "session_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_student_id",
                table: "attendance_records",
                column: "student_id");

            migrationBuilder.AddForeignKey(
                name: "FK_quizzes_modules_module_id",
                table: "quizzes",
                column: "module_id",
                principalTable: "modules",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quizzes_modules_module_id",
                table: "quizzes");

            migrationBuilder.DropTable(
                name: "assignment_test_results");

            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "assignment_submissions");

            migrationBuilder.DropTable(
                name: "assignment_test_cases");

            migrationBuilder.DropTable(
                name: "assignments");

            migrationBuilder.Sql("ALTER TABLE quizzes DROP CONSTRAINT IF EXISTS chk_quiz_pass_score;");

            migrationBuilder.DropColumn(
                name: "disable_copy_paste",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "is_practice",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "max_attempts",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "randomize_questions",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "type",
                table: "quizzes");

            migrationBuilder.DropColumn(
                name: "attempt_number",
                table: "quiz_attempts");

            migrationBuilder.RenameColumn(
                name: "module_id",
                table: "quizzes",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_quizzes_module_id",
                table: "quizzes",
                newName: "IX_quizzes_course_id");

            migrationBuilder.AddColumn<bool>(
                name: "allow_retake",
                table: "quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddForeignKey(
                name: "FK_quizzes_courses_course_id",
                table: "quizzes",
                column: "course_id",
                principalTable: "courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
