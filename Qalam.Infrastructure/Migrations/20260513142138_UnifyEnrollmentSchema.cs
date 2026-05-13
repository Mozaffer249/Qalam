using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyEnrollmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dev-stage clean-out: the unified Enrollment schema is shape-incompatible with the
            // five old enrollment tables. Clear any test rows so the schema migration applies
            // without fighting NOT NULL constraints on CourseSchedules.EnrollmentId.
            // Reference data, courses, teachers, students, and enrollment REQUESTS are untouched.
            migrationBuilder.Sql("DELETE FROM [course].[CourseSchedules];");
            migrationBuilder.Sql("DELETE FROM [course].[GroupEnrollmentMemberPayments];");
            migrationBuilder.Sql("DELETE FROM [dbo].[CourseEnrollmentPayments];");
            migrationBuilder.Sql("DELETE FROM [course].[CourseGroupEnrollmentMembers];");
            migrationBuilder.Sql("DELETE FROM [course].[CourseGroupEnrollments];");
            migrationBuilder.Sql("DELETE FROM [course].[CourseEnrollments];");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseSchedules_CourseEnrollments_CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseSchedules_CourseGroupEnrollments_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropTable(
                name: "CourseEnrollmentPayments");

            migrationBuilder.DropTable(
                name: "GroupEnrollmentMemberPayments",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseEnrollments",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseGroupEnrollmentMembers",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseGroupEnrollments",
                schema: "course");

            migrationBuilder.DropIndex(
                name: "IX_CourseSchedules_CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropIndex(
                name: "IX_CourseSchedules_CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseSchedule_EnrollmentLink",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentRequestId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    LeaderStudentId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByTeacherId = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnrollmentStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_CourseEnrollmentRequests_EnrollmentRequestId",
                        column: x => x.EnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_LeaderStudentId",
                        column: x => x.LeaderStudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Teachers_ApprovedByTeacherId",
                        column: x => x.ApprovedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentParticipants",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EnrollmentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentParticipants_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentParticipants_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentParticipantId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_EnrollmentParticipants_EnrollmentParticipantId",
                        column: x => x.EnrollmentParticipantId,
                        principalSchema: "course",
                        principalTable: "EnrollmentParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_EnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentParticipants_EnrollmentId",
                schema: "course",
                table: "EnrollmentParticipants",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentParticipants_EnrollmentId_StudentId",
                schema: "course",
                table: "EnrollmentParticipants",
                columns: new[] { "EnrollmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentParticipants_PaymentStatus",
                schema: "course",
                table: "EnrollmentParticipants",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentParticipants_StudentId",
                schema: "course",
                table: "EnrollmentParticipants",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_EnrollmentParticipantId",
                table: "EnrollmentPayments",
                column: "EnrollmentParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_PaymentId",
                table: "EnrollmentPayments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_Status",
                table: "EnrollmentPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ApprovedByTeacherId",
                schema: "course",
                table: "Enrollments",
                column: "ApprovedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                schema: "course",
                table: "Enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId_EnrollmentStatus",
                schema: "course",
                table: "Enrollments",
                columns: new[] { "CourseId", "EnrollmentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrollmentRequestId",
                schema: "course",
                table: "Enrollments",
                column: "EnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrollmentStatus_PaymentDeadline",
                schema: "course",
                table: "Enrollments",
                columns: new[] { "EnrollmentStatus", "PaymentDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LeaderStudentId",
                schema: "course",
                table: "Enrollments",
                column: "LeaderStudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSchedules_Enrollments_EnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "EnrollmentId",
                principalSchema: "course",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSchedules_Enrollments_EnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropTable(
                name: "EnrollmentPayments");

            migrationBuilder.DropTable(
                name: "EnrollmentParticipants",
                schema: "course");

            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "course");

            migrationBuilder.DropIndex(
                name: "IX_CourseSchedules_EnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.AddColumn<int>(
                name: "CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseGroupEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseEnrollments",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovedByTeacherId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentRequestId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    EnrollmentStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_CourseEnrollmentRequests_EnrollmentRequestId",
                        column: x => x.EnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Teachers_ApprovedByTeacherId",
                        column: x => x.ApprovedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    PaymentDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "CourseEnrollmentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollmentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentPayments_CourseEnrollments_CourseEnrollmentId",
                        column: x => x.CourseEnrollmentId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentPayments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "IX_CourseSchedules_CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseEnrollmentId");

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

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentPayments_CourseEnrollmentId",
                table: "CourseEnrollmentPayments",
                column: "CourseEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentPayments_PaymentId",
                table: "CourseEnrollmentPayments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_ApprovedByTeacherId",
                schema: "course",
                table: "CourseEnrollments",
                column: "ApprovedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId",
                schema: "course",
                table: "CourseEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId_StudentId",
                schema: "course",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments",
                column: "EnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_EnrollmentStatus",
                schema: "course",
                table: "CourseEnrollments",
                column: "EnrollmentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_EnrollmentStatus_PaymentDeadline",
                schema: "course",
                table: "CourseEnrollments",
                columns: new[] { "EnrollmentStatus", "PaymentDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_StudentId",
                schema: "course",
                table: "CourseEnrollments",
                column: "StudentId");

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
                name: "IX_CourseGroupEnrollments_Status_PaymentDeadline",
                schema: "course",
                table: "CourseGroupEnrollments",
                columns: new[] { "Status", "PaymentDeadline" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSchedules_CourseEnrollments_CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseEnrollmentId",
                principalSchema: "course",
                principalTable: "CourseEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
    }
}
