using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherDomainQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherDomainQuestions",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequirementType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAdminReview = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MinCount = table.Column<int>(type: "int", nullable: false),
                    MaxCount = table.Column<int>(type: "int", nullable: false),
                    MaxFileSizeBytes = table.Column<int>(type: "int", nullable: false),
                    AllowedExtensionsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MapsToDocumentType = table.Column<int>(type: "int", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDomainQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherDomainQuestions_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherDomainQuestionSubmissions",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    TeacherDocumentId = table.Column<int>(type: "int", nullable: true),
                    ReviewedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDomainQuestionSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherDomainQuestionSubmissions_TeacherDocuments_TeacherDocumentId",
                        column: x => x.TeacherDocumentId,
                        principalTable: "TeacherDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeacherDomainQuestionSubmissions_TeacherDomainQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "teacher",
                        principalTable: "TeacherDomainQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDomainQuestionSubmissions_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestions_DomainId_Code",
                schema: "teacher",
                table: "TeacherDomainQuestions",
                columns: new[] { "DomainId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestions_DomainId_IsActive_SortOrder",
                schema: "teacher",
                table: "TeacherDomainQuestions",
                columns: new[] { "DomainId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestionSubmissions_QuestionId",
                schema: "teacher",
                table: "TeacherDomainQuestionSubmissions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestionSubmissions_TeacherDocumentId",
                schema: "teacher",
                table: "TeacherDomainQuestionSubmissions",
                column: "TeacherDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestionSubmissions_TeacherId_QuestionId",
                schema: "teacher",
                table: "TeacherDomainQuestionSubmissions",
                columns: new[] { "TeacherId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainQuestionSubmissions_VerificationStatus",
                schema: "teacher",
                table: "TeacherDomainQuestionSubmissions",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherDomainQuestionSubmissions",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "TeacherDomainQuestions",
                schema: "teacher");
        }
    }
}
