using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "legal");

            migrationBuilder.CreateTable(
                name: "LegalDocuments",
                schema: "legal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequiresConsent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CurrentPublishedVersionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentVersions",
                schema: "legal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalDocumentId = table.Column<int>(type: "int", nullable: false),
                    MajorVersion = table.Column<int>(type: "int", nullable: false),
                    MinorVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChangeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentVersions_LegalDocuments_LegalDocumentId",
                        column: x => x.LegalDocumentId,
                        principalSchema: "legal",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentSections",
                schema: "legal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalDocumentVersionId = table.Column<int>(type: "int", nullable: false),
                    ParentSectionId = table.Column<int>(type: "int", nullable: true),
                    AnchorKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentSections_LegalDocumentSections_ParentSectionId",
                        column: x => x.ParentSectionId,
                        principalSchema: "legal",
                        principalTable: "LegalDocumentSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalDocumentSections_LegalDocumentVersions_LegalDocumentVersionId",
                        column: x => x.LegalDocumentVersionId,
                        principalSchema: "legal",
                        principalTable: "LegalDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLegalConsents",
                schema: "legal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LegalDocumentId = table.Column<int>(type: "int", nullable: false),
                    LegalDocumentVersionId = table.Column<int>(type: "int", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLegalConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLegalConsents_LegalDocumentVersions_LegalDocumentVersionId",
                        column: x => x.LegalDocumentVersionId,
                        principalSchema: "legal",
                        principalTable: "LegalDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLegalConsents_LegalDocuments_LegalDocumentId",
                        column: x => x.LegalDocumentId,
                        principalSchema: "legal",
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLegalConsents_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_Code",
                schema: "legal",
                table: "LegalDocuments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_CurrentPublishedVersionId",
                schema: "legal",
                table: "LegalDocuments",
                column: "CurrentPublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_DisplayOrder",
                schema: "legal",
                table: "LegalDocuments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_IsActive",
                schema: "legal",
                table: "LegalDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentSections_LegalDocumentVersionId",
                schema: "legal",
                table: "LegalDocumentSections",
                column: "LegalDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentSections_LegalDocumentVersionId_AnchorKey",
                schema: "legal",
                table: "LegalDocumentSections",
                columns: new[] { "LegalDocumentVersionId", "AnchorKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentSections_LegalDocumentVersionId_ParentSectionId_DisplayOrder",
                schema: "legal",
                table: "LegalDocumentSections",
                columns: new[] { "LegalDocumentVersionId", "ParentSectionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentSections_ParentSectionId",
                schema: "legal",
                table: "LegalDocumentSections",
                column: "ParentSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_LegalDocumentId_MajorVersion_MinorVersion",
                schema: "legal",
                table: "LegalDocumentVersions",
                columns: new[] { "LegalDocumentId", "MajorVersion", "MinorVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_OnePublished",
                schema: "legal",
                table: "LegalDocumentVersions",
                column: "LegalDocumentId",
                unique: true,
                filter: "[Status] = 'Published'");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_Status",
                schema: "legal",
                table: "LegalDocumentVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_AcceptedAt",
                schema: "legal",
                table: "UserLegalConsents",
                column: "AcceptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_LegalDocumentId",
                schema: "legal",
                table: "UserLegalConsents",
                column: "LegalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_LegalDocumentVersionId",
                schema: "legal",
                table: "UserLegalConsents",
                column: "LegalDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_UserId",
                schema: "legal",
                table: "UserLegalConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_UserId_LegalDocumentVersionId",
                schema: "legal",
                table: "UserLegalConsents",
                columns: new[] { "UserId", "LegalDocumentVersionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LegalDocuments_LegalDocumentVersions_CurrentPublishedVersionId",
                schema: "legal",
                table: "LegalDocuments",
                column: "CurrentPublishedVersionId",
                principalSchema: "legal",
                principalTable: "LegalDocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalDocuments_LegalDocumentVersions_CurrentPublishedVersionId",
                schema: "legal",
                table: "LegalDocuments");

            migrationBuilder.DropTable(
                name: "LegalDocumentSections",
                schema: "legal");

            migrationBuilder.DropTable(
                name: "UserLegalConsents",
                schema: "legal");

            migrationBuilder.DropTable(
                name: "LegalDocumentVersions",
                schema: "legal");

            migrationBuilder.DropTable(
                name: "LegalDocuments",
                schema: "legal");
        }
    }
}
