using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundsEarningsPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsFreeTrial",
                schema: "course",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "course",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayoutBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MockTransferRef = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutBatches_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderRefundId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    InitiatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Users_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayoutItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutBatchId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutItems_PayoutBatches_PayoutBatchId",
                        column: x => x.PayoutBatchId,
                        principalTable: "PayoutBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayoutItems_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherEarningLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayoutItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherEarningLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherEarningLines_CourseSchedules_CourseScheduleId",
                        column: x => x.CourseScheduleId,
                        principalSchema: "course",
                        principalTable: "CourseSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherEarningLines_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherEarningLines_PayoutItems_PayoutItemId",
                        column: x => x.PayoutItemId,
                        principalTable: "PayoutItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherEarningLines_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutBatches_CreatedByUserId",
                table: "PayoutBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutBatches_PeriodStart",
                table: "PayoutBatches",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutBatches_Status",
                table: "PayoutBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutItems_PayoutBatchId",
                table: "PayoutItems",
                column: "PayoutBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutItems_TeacherId",
                table: "PayoutItems",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CreatedAt",
                table: "Refunds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_EnrollmentId",
                table: "Refunds",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_InitiatedByUserId",
                table: "Refunds",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentId",
                table: "Refunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_Status",
                table: "Refunds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEarningLines_CourseScheduleId",
                table: "TeacherEarningLines",
                column: "CourseScheduleId",
                unique: true,
                filter: "[CourseScheduleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEarningLines_EnrollmentId",
                table: "TeacherEarningLines",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEarningLines_PayoutItemId",
                table: "TeacherEarningLines",
                column: "PayoutItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEarningLines_Status",
                table: "TeacherEarningLines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEarningLines_TeacherId",
                table: "TeacherEarningLines",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEarningLines_TeacherId_Status",
                table: "TeacherEarningLines",
                columns: new[] { "TeacherId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "TeacherEarningLines");

            migrationBuilder.DropTable(
                name: "PayoutItems");

            migrationBuilder.DropTable(
                name: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.AlterColumn<bool>(
                name: "IsFreeTrial",
                schema: "course",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }
    }
}
