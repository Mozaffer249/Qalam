using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingAndFixCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSessions",
                schema: "course");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses");

            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.AlterColumn<int>(
                name: "CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedTotalPrice",
                schema: "course",
                table: "CourseEnrollmentRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalMinutes",
                schema: "course",
                table: "CourseEnrollmentRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CourseGroupEnrollments",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentRequestId = table.Column<int>(type: "int", nullable: false),
                    LeaderStudentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGroupEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGroupEnrollments_CourseEnrollmentRequests_EnrollmentRequestId",
                        column: x => x.EnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseGroupEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseGroupEnrollments_Students_LeaderStudentId",
                        column: x => x.LeaderStudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseRequestProposedSessions",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentRequestId = table.Column<int>(type: "int", nullable: false),
                    SessionNumber = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequestProposedSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequestProposedSessions_CourseEnrollmentRequests_CourseEnrollmentRequestId",
                        column: x => x.CourseEnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageLogs",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseGroupEnrollmentMembers",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseGroupEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGroupEnrollmentMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGroupEnrollmentMembers_CourseGroupEnrollments_CourseGroupEnrollmentId",
                        column: x => x.CourseGroupEnrollmentId,
                        principalSchema: "course",
                        principalTable: "CourseGroupEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseGroupEnrollmentMembers_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupEnrollmentMemberPayments",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseGroupEnrollmentMemberId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupEnrollmentMemberPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupEnrollmentMemberPayments_CourseGroupEnrollmentMembers_CourseGroupEnrollmentMemberId",
                        column: x => x.CourseGroupEnrollmentMemberId,
                        principalSchema: "course",
                        principalTable: "CourseGroupEnrollmentMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupEnrollmentMemberPayments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseGroupEnrollmentId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseSchedule_EnrollmentLink",
                schema: "course",
                table: "CourseSchedules",
                sql: "(([CourseEnrollmentId] IS NOT NULL AND [CourseGroupEnrollmentId] IS NULL) OR ([CourseEnrollmentId] IS NULL AND [CourseGroupEnrollmentId] IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_MaxStudents",
                schema: "course",
                table: "Courses",
                sql: "([MaxStudents] IS NULL) OR ([MaxStudents] >= 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses",
                sql: "(([IsFlexible] = 0) AND ([SessionDurationMinutes] IS NOT NULL) AND ([SessionDurationMinutes] > 0)) OR (([IsFlexible] = 1) AND ([SessionDurationMinutes] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses",
                sql: "(([IsFlexible] = 0) AND ([SessionsCount] IS NOT NULL) AND ([SessionsCount] > 0)) OR (([IsFlexible] = 1) AND ([SessionsCount] IS NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollmentMembers_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseGroupEnrollmentMembers",
                column: "CourseGroupEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollmentMembers_CourseGroupEnrollmentId_StudentId",
                schema: "course",
                table: "CourseGroupEnrollmentMembers",
                columns: new[] { "CourseGroupEnrollmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollmentMembers_PaymentStatus",
                schema: "course",
                table: "CourseGroupEnrollmentMembers",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollmentMembers_StudentId",
                schema: "course",
                table: "CourseGroupEnrollmentMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollments_CourseId",
                schema: "course",
                table: "CourseGroupEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollments_EnrollmentRequestId",
                schema: "course",
                table: "CourseGroupEnrollments",
                column: "EnrollmentRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollments_LeaderStudentId",
                schema: "course",
                table: "CourseGroupEnrollments",
                column: "LeaderStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollments_Status",
                schema: "course",
                table: "CourseGroupEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestProposedSessions_CourseEnrollmentRequestId",
                schema: "course",
                table: "CourseRequestProposedSessions",
                column: "CourseEnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestProposedSessions_CourseEnrollmentRequestId_SessionNumber",
                schema: "course",
                table: "CourseRequestProposedSessions",
                columns: new[] { "CourseEnrollmentRequestId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupEnrollmentMemberPayments_CourseGroupEnrollmentMemberId",
                schema: "course",
                table: "GroupEnrollmentMemberPayments",
                column: "CourseGroupEnrollmentMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupEnrollmentMemberPayments_CourseGroupEnrollmentMemberId_PaymentId",
                schema: "course",
                table: "GroupEnrollmentMemberPayments",
                columns: new[] { "CourseGroupEnrollmentMemberId", "PaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupEnrollmentMemberPayments_PaymentId",
                schema: "course",
                table: "GroupEnrollmentMemberPayments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_MessageId",
                schema: "messaging",
                table: "MessageLogs",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_QueuedAt",
                schema: "messaging",
                table: "MessageLogs",
                column: "QueuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_Status",
                schema: "messaging",
                table: "MessageLogs",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSchedules_CourseGroupEnrollments_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseGroupEnrollmentId",
                principalSchema: "course",
                principalTable: "CourseGroupEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSchedules_CourseGroupEnrollments_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropTable(
                name: "CourseRequestProposedSessions",
                schema: "course");

            migrationBuilder.DropTable(
                name: "GroupEnrollmentMemberPayments",
                schema: "course");

            migrationBuilder.DropTable(
                name: "MessageLogs",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "CourseGroupEnrollmentMembers",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseGroupEnrollments",
                schema: "course");

            migrationBuilder.DropIndex(
                name: "IX_CourseSchedules_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseSchedule_EnrollmentLink",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_MaxStudents",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                schema: "course",
                table: "CourseRequestGroupMembers");

            migrationBuilder.DropColumn(
                name: "EstimatedTotalPrice",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.DropColumn(
                name: "TotalMinutes",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.AlterColumn<int>(
                name: "CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CourseSessions",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses",
                sql: "([IsFlexible] = 1) OR ([SessionDurationMinutes] IS NOT NULL AND [SessionDurationMinutes] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses",
                sql: "([IsFlexible] = 1) OR ([SessionsCount] IS NOT NULL AND [SessionsCount] > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseId",
                schema: "course",
                table: "CourseSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseId_SessionNumber",
                schema: "course",
                table: "CourseSessions",
                columns: new[] { "CourseId", "SessionNumber" },
                unique: true);
        }
    }
}
