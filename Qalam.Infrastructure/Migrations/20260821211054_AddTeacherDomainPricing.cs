using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherDomainPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_TeacherId_Status",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions");

            migrationBuilder.CreateTable(
                name: "TeacherDomainPricings",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    TeacherLevelId = table.Column<int>(type: "int", nullable: true),
                    CustomTeacherSharePct = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CustomPricePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReflectCustomPriceToStudent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasCompletedInterviewSession = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDomainPricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherDomainPricings_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDomainPricings_TeacherLevels_TeacherLevelId",
                        column: x => x.TeacherLevelId,
                        principalSchema: "teacher",
                        principalTable: "TeacherLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDomainPricings_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainPricings_DomainId",
                schema: "teacher",
                table: "TeacherDomainPricings",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainPricings_TeacherId_DomainId",
                schema: "teacher",
                table: "TeacherDomainPricings",
                columns: new[] { "TeacherId", "DomainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainPricings_TeacherLevelId",
                schema: "teacher",
                table: "TeacherDomainPricings",
                column: "TeacherLevelId");

            // Backfill one row per (teacher, domain) from subjects; copy global level/share.
            migrationBuilder.Sql("""
                INSERT INTO teacher.TeacherDomainPricings
                    (TeacherId, DomainId, TeacherLevelId, CustomTeacherSharePct, CustomPricePerHour,
                     ReflectCustomPriceToStudent, HasCompletedInterviewSession, CreatedAt)
                SELECT
                    t.Id,
                    s.DomainId,
                    t.TeacherLevelId,
                    t.CustomTeacherSharePct,
                    NULL,
                    0,
                    CASE WHEN t.HasCompletedInterviewSession = 1 AND t.TeacherLevelId IS NOT NULL THEN 1 ELSE 0 END,
                    SYSUTCDATETIME()
                FROM dbo.Teachers t
                INNER JOIN education.TeacherSubjects ts ON ts.TeacherId = t.Id
                INNER JOIN education.Subjects s ON s.Id = ts.SubjectId
                GROUP BY t.Id, s.DomainId, t.TeacherLevelId, t.CustomTeacherSharePct, t.HasCompletedInterviewSession;
                """);

            migrationBuilder.AddColumn<int>(
                name: "DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                type: "int",
                nullable: true);

            // Assign existing suggestions to the teacher's first domain pricing row (or first subject domain).
            migrationBuilder.Sql("""
                UPDATE sug
                SET sug.DomainId = d.DomainId
                FROM teacher.TeacherLevelUpgradeSuggestions sug
                CROSS APPLY (
                    SELECT TOP (1) tdp.DomainId
                    FROM teacher.TeacherDomainPricings tdp
                    WHERE tdp.TeacherId = sug.TeacherId
                    ORDER BY tdp.DomainId
                ) d
                WHERE sug.DomainId IS NULL;

                DELETE FROM teacher.TeacherLevelUpgradeSuggestions WHERE DomainId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_TeacherId_DomainId_Status",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                columns: new[] { "TeacherId", "DomainId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherLevelUpgradeSuggestions_EducationDomains_DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                column: "DomainId",
                principalSchema: "education",
                principalTable: "EducationDomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherLevelUpgradeSuggestions_EducationDomains_DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions");

            migrationBuilder.DropTable(
                name: "TeacherDomainPricings",
                schema: "teacher");

            migrationBuilder.DropIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions");

            migrationBuilder.DropIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_TeacherId_DomainId_Status",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions");

            migrationBuilder.DropColumn(
                name: "DomainId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_TeacherId_Status",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                columns: new[] { "TeacherId", "Status" });
        }
    }
}
