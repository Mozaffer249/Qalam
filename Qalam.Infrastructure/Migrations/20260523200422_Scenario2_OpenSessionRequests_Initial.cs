using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Scenario2_OpenSessionRequests_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Courses_CourseId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.EnsureSchema(
                name: "sr");

            // Idempotent — migration may retry after partial apply (SessionOfferId already on Enrollments).
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.columns c
                    INNER JOIN sys.tables t ON c.object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = N'course' AND t.name = N'Enrollments'
                      AND c.name = N'CourseId' AND c.is_nullable = 0)
                BEGIN
                    ALTER TABLE [course].[Enrollments] ALTER COLUMN [CourseId] int NULL;
                END

                IF COL_LENGTH('course.Enrollments', 'SessionOfferId') IS NULL
                    ALTER TABLE [course].[Enrollments] ADD [SessionOfferId] int NULL;

                IF COL_LENGTH('course.Enrollments', 'SessionRequestId') IS NULL
                    ALTER TABLE [course].[Enrollments] ADD [SessionRequestId] int NULL;

                IF COL_LENGTH('course.Enrollments', 'Source') IS NULL
                    ALTER TABLE [course].[Enrollments] ADD [Source] int NOT NULL
                        CONSTRAINT [DF_Enrollments_Source_Scenario2] DEFAULT 1;
                """);

            migrationBuilder.CreateTable(
                name: "SessionRequests",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedByGuardianId = table.Column<int>(type: "int", nullable: true),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    LevelId = table.Column<int>(type: "int", nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    TermId = table.Column<int>(type: "int", nullable: true),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    GroupType = table.Column<int>(type: "int", nullable: true),
                    TotalSessionsCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StudentNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequests_AcademicTerms_TermId",
                        column: x => x.TermId,
                        principalSchema: "education",
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "education",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Guardians_CreatedByGuardianId",
                        column: x => x.CreatedByGuardianId,
                        principalSchema: "student",
                        principalTable: "Guardians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "education",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_TeachingModes_TeachingModeId",
                        column: x => x.TeachingModeId,
                        principalSchema: "teaching",
                        principalTable: "TeachingModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionOffers",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GroupType = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WithdrawnAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeacherNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionOffers_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "sr",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionOffers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequestAttachments",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequestAttachments_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "sr",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequestInvitations",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    InvitedStudentId = table.Column<int>(type: "int", nullable: false),
                    InvitedByStudentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequestInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequestInvitations_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "sr",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionRequestInvitations_Students_InvitedByStudentId",
                        column: x => x.InvitedByStudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequestInvitations_Students_InvitedStudentId",
                        column: x => x.InvitedStudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequestSessions",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    PreferredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TimeSlotId = table.Column<int>(type: "int", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    QuranContentTypeId = table.Column<int>(type: "int", nullable: true),
                    QuranLevelId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessions_QuranContentTypes_QuranContentTypeId",
                        column: x => x.QuranContentTypeId,
                        principalSchema: "quran",
                        principalTable: "QuranContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessions_QuranLevels_QuranLevelId",
                        column: x => x.QuranLevelId,
                        principalSchema: "quran",
                        principalTable: "QuranLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessions_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "sr",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessions_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalSchema: "common",
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequestTargets",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequestTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequestTargets_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "sr",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionRequestTargets_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferConversations",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionOfferId = table.Column<int>(type: "int", nullable: false),
                    StudentLastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeacherLastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferConversations_SessionOffers_SessionOfferId",
                        column: x => x.SessionOfferId,
                        principalSchema: "sr",
                        principalTable: "SessionOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequestSessionUnits",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestSessionId = table.Column<int>(type: "int", nullable: false),
                    ContentUnitId = table.Column<int>(type: "int", nullable: true),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequestSessionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessionUnits_ContentUnits_ContentUnitId",
                        column: x => x.ContentUnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessionUnits_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalSchema: "education",
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequestSessionUnits_SessionRequestSessions_SessionRequestSessionId",
                        column: x => x.SessionRequestSessionId,
                        principalSchema: "sr",
                        principalTable: "SessionRequestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfferMessages",
                schema: "sr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferMessages_OfferConversations_OfferConversationId",
                        column: x => x.OfferConversationId,
                        principalSchema: "sr",
                        principalTable: "OfferConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SessionOfferId",
                schema: "course",
                table: "Enrollments",
                column: "SessionOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SessionRequestId",
                schema: "course",
                table: "Enrollments",
                column: "SessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Source_EnrollmentStatus",
                schema: "course",
                table: "Enrollments",
                columns: new[] { "Source", "EnrollmentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_LastMessageAt",
                schema: "sr",
                table: "OfferConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_SessionOfferId",
                schema: "sr",
                table: "OfferConversations",
                column: "SessionOfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferMessages_OfferConversationId_SentAt",
                schema: "sr",
                table: "OfferMessages",
                columns: new[] { "OfferConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferMessages_SenderUserId",
                schema: "sr",
                table: "OfferMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_SessionRequestId_TeacherId",
                schema: "sr",
                table: "SessionOffers",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true,
                filter: "[Status] <> 5");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_Status",
                schema: "sr",
                table: "SessionOffers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_Status_ExpiresAt",
                schema: "sr",
                table: "SessionOffers",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_TeacherId",
                schema: "sr",
                table: "SessionOffers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestAttachments_SessionRequestId",
                schema: "sr",
                table: "SessionRequestAttachments",
                column: "SessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestInvitations_InvitedByStudentId",
                schema: "sr",
                table: "SessionRequestInvitations",
                column: "InvitedByStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestInvitations_InvitedStudentId",
                schema: "sr",
                table: "SessionRequestInvitations",
                column: "InvitedStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestInvitations_SessionRequestId_InvitedStudentId",
                schema: "sr",
                table: "SessionRequestInvitations",
                columns: new[] { "SessionRequestId", "InvitedStudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestInvitations_Status",
                schema: "sr",
                table: "SessionRequestInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_CreatedByGuardianId",
                schema: "sr",
                table: "SessionRequests",
                column: "CreatedByGuardianId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_CurriculumId",
                schema: "sr",
                table: "SessionRequests",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_DomainId",
                schema: "sr",
                table: "SessionRequests",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_GradeId",
                schema: "sr",
                table: "SessionRequests",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_LevelId",
                schema: "sr",
                table: "SessionRequests",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_RequestedByUserId",
                schema: "sr",
                table: "SessionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_Status",
                schema: "sr",
                table: "SessionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_Status_ExpiresAt",
                schema: "sr",
                table: "SessionRequests",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_StudentId",
                schema: "sr",
                table: "SessionRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_SubjectId",
                schema: "sr",
                table: "SessionRequests",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_TeachingModeId",
                schema: "sr",
                table: "SessionRequests",
                column: "TeachingModeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_TermId",
                schema: "sr",
                table: "SessionRequests",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessions_PreferredDate",
                schema: "sr",
                table: "SessionRequestSessions",
                column: "PreferredDate");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessions_QuranContentTypeId",
                schema: "sr",
                table: "SessionRequestSessions",
                column: "QuranContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessions_QuranLevelId",
                schema: "sr",
                table: "SessionRequestSessions",
                column: "QuranLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessions_SessionRequestId_SequenceNumber",
                schema: "sr",
                table: "SessionRequestSessions",
                columns: new[] { "SessionRequestId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessions_TimeSlotId",
                schema: "sr",
                table: "SessionRequestSessions",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessionUnits_ContentUnitId",
                schema: "sr",
                table: "SessionRequestSessionUnits",
                column: "ContentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessionUnits_LessonId",
                schema: "sr",
                table: "SessionRequestSessionUnits",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestSessionUnits_SessionRequestSessionId",
                schema: "sr",
                table: "SessionRequestSessionUnits",
                column: "SessionRequestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestTargets_SessionRequestId_TeacherId",
                schema: "sr",
                table: "SessionRequestTargets",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestTargets_TeacherId",
                schema: "sr",
                table: "SessionRequestTargets",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestTargets_TeacherId_Status",
                schema: "sr",
                table: "SessionRequestTargets",
                columns: new[] { "TeacherId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Courses_CourseId",
                schema: "course",
                table: "Enrollments",
                column: "CourseId",
                principalSchema: "course",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_SessionOffers_SessionOfferId",
                schema: "course",
                table: "Enrollments",
                column: "SessionOfferId",
                principalSchema: "sr",
                principalTable: "SessionOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_SessionRequests_SessionRequestId",
                schema: "course",
                table: "Enrollments",
                column: "SessionRequestId",
                principalSchema: "sr",
                principalTable: "SessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Courses_CourseId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_SessionOffers_SessionOfferId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_SessionRequests_SessionRequestId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Curriculums_CurriculumId",
                schema: "session",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_EducationLevels_LevelId",
                schema: "session",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Students_StudentId",
                schema: "session",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Subjects_SubjectId",
                schema: "session",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_TeachingModes_TeachingModeId",
                schema: "session",
                table: "SessionRequests");

            migrationBuilder.DropTable(
                name: "OfferMessages",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionRequestAttachments",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionRequestInvitations",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionRequestSessionUnits",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionRequestTargets",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "OfferConversations",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionRequestSessions",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionOffers",
                schema: "sr");

            migrationBuilder.DropTable(
                name: "SessionRequests",
                schema: "sr");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SessionOfferId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SessionRequestId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_Source_EnrollmentStatus",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "SessionOfferId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "SessionRequestId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.AlterColumn<int>(
                name: "CourseId",
                schema: "course",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Courses_CourseId",
                schema: "course",
                table: "Enrollments",
                column: "CourseId",
                principalSchema: "course",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
