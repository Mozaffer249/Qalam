using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomTeacherSharePct",
                table: "Teachers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherLevelId",
                table: "Teachers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingSnapshotId",
                schema: "sr",
                table: "SessionOffers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingSnapshotId",
                schema: "course",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingSnapshotId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DomainSessionPrices",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    SessionTypeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainSessionPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DomainSessionPrices_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherLevels",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    TeacherSharePct = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingSnapshots",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Context = table.Column<int>(type: "int", nullable: false),
                    ContextEntityId = table.Column<int>(type: "int", nullable: false),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    SessionTypeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DomainSessionPriceId = table.Column<int>(type: "int", nullable: true),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    TeacherLevelId = table.Column<int>(type: "int", nullable: true),
                    TeacherSharePct = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TeacherEarnings = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingSnapshots_DomainSessionPrices_DomainSessionPriceId",
                        column: x => x.DomainSessionPriceId,
                        principalSchema: "pricing",
                        principalTable: "DomainSessionPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeacherLevelUpgradeSuggestions",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    CurrentLevelId = table.Column<int>(type: "int", nullable: false),
                    SuggestedLevelId = table.Column<int>(type: "int", nullable: false),
                    AvgRating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    CompletedSessions = table.Column<int>(type: "int", nullable: false),
                    AttendanceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherLevelUpgradeSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherLevelUpgradeSuggestions_TeacherLevels_CurrentLevelId",
                        column: x => x.CurrentLevelId,
                        principalSchema: "teacher",
                        principalTable: "TeacherLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherLevelUpgradeSuggestions_TeacherLevels_SuggestedLevelId",
                        column: x => x.SuggestedLevelId,
                        principalSchema: "teacher",
                        principalTable: "TeacherLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherLevelUpgradeSuggestions_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TeacherLevelId",
                table: "Teachers",
                column: "TeacherLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_PricingSnapshotId",
                schema: "sr",
                table: "SessionOffers",
                column: "PricingSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PricingSnapshotId",
                schema: "course",
                table: "Enrollments",
                column: "PricingSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentRequests_PricingSnapshotId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "PricingSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_DomainId_SessionTypeCode_EffectiveFrom",
                schema: "pricing",
                table: "DomainSessionPrices",
                columns: new[] { "DomainId", "SessionTypeCode", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_DomainId_SessionTypeCode_EffectiveTo",
                schema: "pricing",
                table: "DomainSessionPrices",
                columns: new[] { "DomainId", "SessionTypeCode", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingSnapshots_Context_ContextEntityId",
                schema: "pricing",
                table: "PricingSnapshots",
                columns: new[] { "Context", "ContextEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingSnapshots_DomainSessionPriceId",
                schema: "pricing",
                table: "PricingSnapshots",
                column: "DomainSessionPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevels_Code",
                schema: "teacher",
                table: "TeacherLevels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevels_OrderIndex",
                schema: "teacher",
                table: "TeacherLevels",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_CurrentLevelId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                column: "CurrentLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_SuggestedLevelId",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                column: "SuggestedLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLevelUpgradeSuggestions_TeacherId_Status",
                schema: "teacher",
                table: "TeacherLevelUpgradeSuggestions",
                columns: new[] { "TeacherId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollmentRequests_PricingSnapshots_PricingSnapshotId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "PricingSnapshotId",
                principalSchema: "pricing",
                principalTable: "PricingSnapshots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_PricingSnapshots_PricingSnapshotId",
                schema: "course",
                table: "Enrollments",
                column: "PricingSnapshotId",
                principalSchema: "pricing",
                principalTable: "PricingSnapshots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionOffers_PricingSnapshots_PricingSnapshotId",
                schema: "sr",
                table: "SessionOffers",
                column: "PricingSnapshotId",
                principalSchema: "pricing",
                principalTable: "PricingSnapshots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_TeacherLevels_TeacherLevelId",
                table: "Teachers",
                column: "TeacherLevelId",
                principalSchema: "teacher",
                principalTable: "TeacherLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollmentRequests_PricingSnapshots_PricingSnapshotId",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_PricingSnapshots_PricingSnapshotId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionOffers_PricingSnapshots_PricingSnapshotId",
                schema: "sr",
                table: "SessionOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_TeacherLevels_TeacherLevelId",
                table: "Teachers");

            migrationBuilder.DropTable(
                name: "PricingSnapshots",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "TeacherLevelUpgradeSuggestions",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "DomainSessionPrices",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "TeacherLevels",
                schema: "teacher");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_TeacherLevelId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_SessionOffers_PricingSnapshotId",
                schema: "sr",
                table: "SessionOffers");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_PricingSnapshotId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollmentRequests_PricingSnapshotId",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.DropColumn(
                name: "CustomTeacherSharePct",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "TeacherLevelId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "PricingSnapshotId",
                schema: "sr",
                table: "SessionOffers");

            migrationBuilder.DropColumn(
                name: "PricingSnapshotId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PricingSnapshotId",
                schema: "course",
                table: "CourseEnrollmentRequests");
        }
    }
}
