using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedComplaintHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "complaint");

            migrationBuilder.CreateTable(
                name: "Complaints",
                schema: "complaint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: true),
                    EnrollmentId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    RefundId = table.Column<int>(type: "int", nullable: true),
                    OpenSessionRequestId = table.Column<int>(type: "int", nullable: true),
                    ComplainantUserId = table.Column<int>(type: "int", nullable: false),
                    ComplainantRole = table.Column<int>(type: "int", nullable: false),
                    AffectedStudentId = table.Column<int>(type: "int", nullable: true),
                    RespondentTeacherId = table.Column<int>(type: "int", nullable: true),
                    RespondentUserId = table.Column<int>(type: "int", nullable: true),
                    ReasonCode = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    FiledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    ResolutionCode = table.Column<int>(type: "int", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    RequiresRespondentResponse = table.Column<bool>(type: "bit", nullable: false),
                    RequiresComplainantResponse = table.Column<bool>(type: "bit", nullable: false),
                    RespondentResponse = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RespondentRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComplainantResponse = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ComplainantRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LinkedRefundId = table.Column<int>(type: "int", nullable: true),
                    ReplacementScheduleId = table.Column<int>(type: "int", nullable: true),
                    LegacySessionComplaintId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintAttachments",
                schema: "complaint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintAttachments_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalSchema: "complaint",
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintTimelineEntries",
                schema: "complaint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: true),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintTimelineEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintTimelineEntries_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalSchema: "complaint",
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintAttachments_ComplaintId",
                schema: "complaint",
                table: "ComplaintAttachments",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AssignedToUserId",
                schema: "complaint",
                table: "Complaints",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ComplainantUserId",
                schema: "complaint",
                table: "Complaints",
                column: "ComplainantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CourseScheduleId",
                schema: "complaint",
                table: "Complaints",
                column: "CourseScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CourseScheduleId_AffectedStudentId_Status",
                schema: "complaint",
                table: "Complaints",
                columns: new[] { "CourseScheduleId", "AffectedStudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_EnrollmentId",
                schema: "complaint",
                table: "Complaints",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_LegacySessionComplaintId",
                schema: "complaint",
                table: "Complaints",
                column: "LegacySessionComplaintId",
                unique: true,
                filter: "[LegacySessionComplaintId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_OpenSessionRequestId",
                schema: "complaint",
                table: "Complaints",
                column: "OpenSessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_PaymentId",
                schema: "complaint",
                table: "Complaints",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_Priority",
                schema: "complaint",
                table: "Complaints",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_RefundId",
                schema: "complaint",
                table: "Complaints",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_Status",
                schema: "complaint",
                table: "Complaints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_SubjectType",
                schema: "complaint",
                table: "Complaints",
                column: "SubjectType");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_SubjectType_Status_FiledAt",
                schema: "complaint",
                table: "Complaints",
                columns: new[] { "SubjectType", "Status", "FiledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintTimelineEntries_ComplaintId",
                schema: "complaint",
                table: "ComplaintTimelineEntries",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintTimelineEntries_ComplaintId_OccurredAt",
                schema: "complaint",
                table: "ComplaintTimelineEntries",
                columns: new[] { "ComplaintId", "OccurredAt" });

            // Copy legacy session complaints into the unified hub (preserve IDs).
            migrationBuilder.Sql("""
                SET IDENTITY_INSERT [complaint].[Complaints] ON;

                INSERT INTO [complaint].[Complaints] (
                    [Id], [SubjectType], [CourseScheduleId], [EnrollmentId], [PaymentId], [RefundId], [OpenSessionRequestId],
                    [ComplainantUserId], [ComplainantRole], [AffectedStudentId], [RespondentTeacherId], [RespondentUserId],
                    [ReasonCode], [Description], [Status], [Priority], [FiledAt], [ResolvedAt], [ResolvedByUserId],
                    [ResolutionCode], [ResolutionNotes], [AssignedToUserId],
                    [RequiresRespondentResponse], [RequiresComplainantResponse],
                    [RespondentResponse], [RespondentRespondedAt], [ComplainantResponse], [ComplainantRespondedAt],
                    [LinkedRefundId], [ReplacementScheduleId], [LegacySessionComplaintId],
                    [CreatedAt], [UpdatedAt], [CreatedBy], [UpdatedBy]
                )
                SELECT
                    sc.[Id],
                    1,
                    sc.[CourseScheduleId],
                    sc.[EnrollmentId],
                    NULL,
                    NULL,
                    NULL,
                    ISNULL(s.[UserId], 0),
                    1,
                    sc.[StudentId],
                    sc.[TeacherId],
                    NULL,
                    CASE WHEN sc.[ReasonCode] BETWEEN 1 AND 6 THEN sc.[ReasonCode] ELSE 10 END,
                    sc.[Description],
                    CASE sc.[Status]
                        WHEN 1 THEN 1
                        WHEN 2 THEN 2
                        WHEN 3 THEN 4
                        WHEN 4 THEN 3
                        WHEN 5 THEN 6
                        WHEN 6 THEN 7
                        ELSE 1
                    END,
                    1,
                    sc.[FiledAt],
                    sc.[ResolvedAt],
                    sc.[ResolvedByUserId],
                    sc.[ResolutionCode],
                    sc.[ResolutionNotes],
                    sc.[AssignedToUserId],
                    sc.[RequiresTeacherResponse],
                    0,
                    sc.[TeacherResponse],
                    sc.[TeacherRespondedAt],
                    NULL,
                    NULL,
                    sc.[RefundId],
                    sc.[ReplacementScheduleId],
                    sc.[Id],
                    sc.[CreatedAt],
                    sc.[UpdatedAt],
                    sc.[CreatedBy],
                    sc.[UpdatedBy]
                FROM [course].[SessionComplaints] sc
                LEFT JOIN [student].[Students] s ON s.[Id] = sc.[StudentId];

                SET IDENTITY_INSERT [complaint].[Complaints] OFF;

                SET IDENTITY_INSERT [complaint].[ComplaintAttachments] ON;

                INSERT INTO [complaint].[ComplaintAttachments] (
                    [Id], [ComplaintId], [FileUrl], [FileName], [ContentType], [UploadedByUserId], [UploadedAt],
                    [CreatedAt], [UpdatedAt], [CreatedBy], [UpdatedBy]
                )
                SELECT
                    a.[Id], a.[ComplaintId], a.[FileUrl], a.[FileName], a.[ContentType], a.[UploadedByUserId], a.[UploadedAt],
                    a.[CreatedAt], a.[UpdatedAt], a.[CreatedBy], a.[UpdatedBy]
                FROM [course].[SessionComplaintAttachments] a
                INNER JOIN [complaint].[Complaints] c ON c.[LegacySessionComplaintId] = a.[ComplaintId];

                SET IDENTITY_INSERT [complaint].[ComplaintAttachments] OFF;

                INSERT INTO [complaint].[ComplaintTimelineEntries] (
                    [ComplaintId], [EventType], [FromStatus], [ToStatus], [ActorUserId], [ActorRole], [Notes], [PayloadJson],
                    [OccurredAt], [CreatedAt]
                )
                SELECT
                    c.[Id], 1, NULL, c.[Status], c.[ComplainantUserId], N'Student', N'Migrated from SessionComplaint', NULL,
                    c.[FiledAt], c.[FiledAt]
                FROM [complaint].[Complaints] c
                WHERE c.[LegacySessionComplaintId] IS NOT NULL;

                INSERT INTO [complaint].[ComplaintTimelineEntries] (
                    [ComplaintId], [EventType], [FromStatus], [ToStatus], [ActorUserId], [ActorRole], [Notes], [PayloadJson],
                    [OccurredAt], [CreatedAt]
                )
                SELECT
                    c.[Id],
                    CASE WHEN c.[Status] = 7 THEN 9 ELSE 8 END,
                    2,
                    c.[Status],
                    ISNULL(c.[ResolvedByUserId], 0),
                    N'Admin',
                    c.[ResolutionNotes],
                    NULL,
                    ISNULL(c.[ResolvedAt], c.[FiledAt]),
                    ISNULL(c.[ResolvedAt], c.[FiledAt])
                FROM [complaint].[Complaints] c
                WHERE c.[LegacySessionComplaintId] IS NOT NULL
                  AND c.[Status] IN (6, 7);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintAttachments",
                schema: "complaint");

            migrationBuilder.DropTable(
                name: "ComplaintTimelineEntries",
                schema: "complaint");

            migrationBuilder.DropTable(
                name: "Complaints",
                schema: "complaint");
        }
    }
}
