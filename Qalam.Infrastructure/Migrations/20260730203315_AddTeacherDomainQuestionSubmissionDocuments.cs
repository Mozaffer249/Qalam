using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherDomainQuestionSubmissionDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherDomainQuestionSubmissionDocuments",
                schema: "teacher",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    TeacherDocumentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDomainQuestionSubmissionDocuments", x => new { x.SubmissionId, x.TeacherDocumentId });
                    table.ForeignKey(
                        name: "FK_TeacherDomainQuestionSubmissionDocuments_TeacherDocuments_TeacherDocumentId",
                        column: x => x.TeacherDocumentId,
                        principalTable: "TeacherDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDomainQuestionSubmissionDocuments_TeacherDomainQuestionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "teacher",
                        principalTable: "TeacherDomainQuestionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestionSubmissionDocuments_TeacherDocumentId",
                schema: "teacher",
                table: "TeacherDomainQuestionSubmissionDocuments",
                column: "TeacherDocumentId",
                unique: true);

            // Backfill primary document links so existing single-file answers appear in the junction.
            migrationBuilder.Sql("""
                INSERT INTO teacher.TeacherDomainQuestionSubmissionDocuments (SubmissionId, TeacherDocumentId)
                SELECT s.Id, s.TeacherDocumentId
                FROM teacher.TeacherDomainQuestionSubmissions s
                WHERE s.TeacherDocumentId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM teacher.TeacherDomainQuestionSubmissionDocuments d
                      WHERE d.SubmissionId = s.Id AND d.TeacherDocumentId = s.TeacherDocumentId
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherDomainQuestionSubmissionDocuments",
                schema: "teacher");
        }
    }
}
