using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationRefundPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceBefore",
                table: "WalletTransactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PolicyCaseId",
                table: "WalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversedByTransactionId",
                table: "WalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WalletTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PolicyCaseId",
                table: "TeacherBalanceAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "Refunds",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PolicyCaseId",
                table: "Refunds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyVersionId",
                schema: "course",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationReason",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyCaseId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplacesScheduleId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PolicyVersions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyCases",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    RefundId = table.Column<int>(type: "int", nullable: true),
                    ReplacementScheduleId = table.Column<int>(type: "int", nullable: true),
                    ComplaintId = table.Column<int>(type: "int", nullable: true),
                    ReversesCaseId = table.Column<int>(type: "int", nullable: true),
                    ReversedByCaseId = table.Column<int>(type: "int", nullable: true),
                    PolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    RuleSectionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExplanationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrossValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TeacherEarningImpact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformRevenueImpact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Destination = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyCases_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PolicyCases_PolicyVersions_PolicyVersionId",
                        column: x => x.PolicyVersionId,
                        principalSchema: "finance",
                        principalTable: "PolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_PolicyCaseId",
                table: "WalletTransactions",
                column: "PolicyCaseId",
                filter: "[PolicyCaseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PolicyCaseId",
                table: "Refunds",
                column: "PolicyCaseId",
                filter: "[PolicyCaseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PolicyVersionId",
                schema: "course",
                table: "Enrollments",
                column: "PolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyCases_CourseScheduleId",
                schema: "finance",
                table: "PolicyCases",
                column: "CourseScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyCases_EnrollmentId",
                schema: "finance",
                table: "PolicyCases",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyCases_Kind_CourseScheduleId",
                schema: "finance",
                table: "PolicyCases",
                columns: new[] { "Kind", "CourseScheduleId" },
                unique: true,
                filter: "[CourseScheduleId] IS NOT NULL AND [Kind] IN (4, 5, 3, 7)");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyCases_Kind_CreatedAt",
                schema: "finance",
                table: "PolicyCases",
                columns: new[] { "Kind", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyCases_PolicyVersionId",
                schema: "finance",
                table: "PolicyCases",
                column: "PolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyVersions_Status_EffectiveFrom",
                schema: "finance",
                table: "PolicyVersions",
                columns: new[] { "Status", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyVersions_VersionNumber",
                schema: "finance",
                table: "PolicyVersions",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_PolicyVersions_PolicyVersionId",
                schema: "course",
                table: "Enrollments",
                column: "PolicyVersionId",
                principalSchema: "finance",
                principalTable: "PolicyVersions",
                principalColumn: "Id");

            migrationBuilder.Sql("UPDATE [WalletTransactions] SET [BalanceBefore] = [BalanceAfter] - [Amount];");

            var rulesJson = Qalam.Data.DTOs.Policy.CancellationPolicyDefaults
                .ToJson(Qalam.Data.DTOs.Policy.CancellationPolicyDefaults.Create())
                .Replace("'", "''");
            migrationBuilder.Sql($@"
INSERT INTO [finance].[PolicyVersions]
    ([VersionNumber], [Status], [EffectiveFrom], [EffectiveTo], [RulesJson], [ChangeNote], [PublishedAt], [CreatedAt])
VALUES
    (1, {(int)Qalam.Data.Entity.Common.Enums.PolicyVersionStatus.Published}, '2000-01-01', NULL, N'{rulesJson}', N'Initial policy (v1)', SYSUTCDATETIME(), SYSUTCDATETIME());

UPDATE [course].[Enrollments]
SET [PolicyVersionId] = (SELECT TOP 1 [Id] FROM [finance].[PolicyVersions] WHERE [VersionNumber] = 1)
WHERE [PolicyVersionId] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_PolicyVersions_PolicyVersionId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "PolicyCases",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "PolicyVersions",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_PolicyCaseId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Refunds_PolicyCaseId",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_PolicyVersionId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "BalanceBefore",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "PolicyCaseId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ReversedByTransactionId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "PolicyCaseId",
                table: "TeacherBalanceAdjustments");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "PolicyCaseId",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "PolicyVersionId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "PolicyCaseId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "ReplacesScheduleId",
                schema: "course",
                table: "CourseSchedules");
        }
    }
}
