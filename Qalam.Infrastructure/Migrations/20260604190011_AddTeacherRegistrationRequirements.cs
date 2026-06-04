using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherRegistrationRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherRegistrationRequirements",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequirementType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MinCount = table.Column<int>(type: "int", nullable: false),
                    MaxCount = table.Column<int>(type: "int", nullable: false),
                    MaxFileSizeBytes = table.Column<int>(type: "int", nullable: false),
                    AllowedExtensionsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    MapsToDocumentType = table.Column<int>(type: "int", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherRegistrationRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherRegistrationSubmissions",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    RequirementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TeacherRegistrationSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherRegistrationSubmissions_TeacherDocuments_TeacherDocumentId",
                        column: x => x.TeacherDocumentId,
                        principalTable: "TeacherDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeacherRegistrationSubmissions_TeacherRegistrationRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalSchema: "teacher",
                        principalTable: "TeacherRegistrationRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherRegistrationSubmissions_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationRequirements_Code",
                schema: "teacher",
                table: "TeacherRegistrationRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationRequirements_IsActive_SortOrder",
                schema: "teacher",
                table: "TeacherRegistrationRequirements",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationSubmissions_RequirementId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationSubmissions_TeacherDocumentId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions",
                column: "TeacherDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationSubmissions_TeacherId_RequirementId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions",
                columns: new[] { "TeacherId", "RequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationSubmissions_VerificationStatus",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherRegistrationSubmissions",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "TeacherRegistrationRequirements",
                schema: "teacher");
        }
    }
}
