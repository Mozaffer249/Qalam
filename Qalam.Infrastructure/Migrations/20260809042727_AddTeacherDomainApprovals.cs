using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherDomainApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherDomainApprovals",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByAdminId = table.Column<int>(type: "int", nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDomainApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherDomainApprovals_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDomainApprovals_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainApprovals_DomainId",
                schema: "teacher",
                table: "TeacherDomainApprovals",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainApprovals_RevokedAt",
                schema: "teacher",
                table: "TeacherDomainApprovals",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainApprovals_TeacherId_DomainId",
                schema: "teacher",
                table: "TeacherDomainApprovals",
                columns: new[] { "TeacherId", "DomainId" },
                unique: true);

            // Backfill: teacher/domain pairs whose answers are already fully approved
            // so already-authorized teachers keep working under the new gate.
            migrationBuilder.Sql("""
                INSERT INTO teacher.TeacherDomainApprovals
                    (TeacherId, DomainId, ApprovedByAdminId, ApprovedAt, CreatedAt)
                SELECT pairs.TeacherId, pairs.DomainId, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM (
                    SELECT DISTINCT s.TeacherId, q.DomainId
                    FROM teacher.TeacherDomainQuestionSubmissions s
                    INNER JOIN teacher.TeacherDomainQuestions q ON q.Id = s.QuestionId
                ) pairs
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM teacher.TeacherDomainQuestions q
                    WHERE q.DomainId = pairs.DomainId
                      AND q.IsActive = 1
                      AND q.IsRequired = 1
                      AND NOT EXISTS (
                          SELECT 1
                          FROM teacher.TeacherDomainQuestionSubmissions s
                          WHERE s.TeacherId = pairs.TeacherId
                            AND s.QuestionId = q.Id
                            AND s.VerificationStatus = 2
                      )
                )
                AND NOT EXISTS (
                    SELECT 1
                    FROM teacher.TeacherDomainQuestionSubmissions s
                    INNER JOIN teacher.TeacherDomainQuestions q ON q.Id = s.QuestionId
                    WHERE s.TeacherId = pairs.TeacherId
                      AND q.DomainId = pairs.DomainId
                      AND s.VerificationStatus <> 2
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherDomainApprovals",
                schema: "teacher");
        }
    }
}
