using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAuditAndReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.AddColumn<string>(
                name: "ProviderInvoiceId",
                table: "Payments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            // Best-effort: Pending/Cancelled Moyasar hosted rows still hold invoice id in ProviderTransactionId.
            migrationBuilder.Sql("""
                UPDATE Payments
                SET ProviderInvoiceId = ProviderTransactionId
                WHERE PaymentProvider = N'Moyasar'
                  AND ProviderInvoiceId IS NULL
                  AND ProviderTransactionId IS NOT NULL
                  AND Status IN (1, 4);
                """);

            migrationBuilder.CreateTable(
                name: "PaymentReconciliationRuns",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduleKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    LookbackFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LookbackToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaginationCursor = table.Column<int>(type: "int", nullable: true),
                    RemotePaymentsSeen = table.Column<int>(type: "int", nullable: false),
                    RemoteInvoicesSeen = table.Column<int>(type: "int", nullable: false),
                    MatchedCount = table.Column<int>(type: "int", nullable: false),
                    RepairedCount = table.Column<int>(type: "int", nullable: false),
                    MismatchCount = table.Column<int>(type: "int", nullable: false),
                    UnresolvedRemoteCount = table.Column<int>(type: "int", nullable: false),
                    MissingRemoteCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactionEvents",
                schema: "payment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    EnrollmentId = table.Column<int>(type: "int", nullable: true),
                    EnrollmentParticipantId = table.Column<int>(type: "int", nullable: true),
                    EnrollmentRequestId = table.Column<int>(type: "int", nullable: true),
                    OpenSessionRequestId = table.Column<int>(type: "int", nullable: true),
                    PaymentProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    StatusBefore = table.Column<int>(type: "int", nullable: true),
                    StatusAfter = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderInvoiceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderEventId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactionEvents_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentProvider_ProviderInvoiceId",
                table: "Payments",
                columns: new[] { "PaymentProvider", "ProviderInvoiceId" },
                filter: "[ProviderInvoiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentProvider_ProviderTransactionId",
                table: "Payments",
                columns: new[] { "PaymentProvider", "ProviderTransactionId" },
                filter: "[ProviderTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationRuns_ScheduleKey",
                schema: "payment",
                table: "PaymentReconciliationRuns",
                column: "ScheduleKey",
                unique: true,
                filter: "[ScheduleKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationRuns_StartedAt",
                schema: "payment",
                table: "PaymentReconciliationRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationRuns_Status",
                schema: "payment",
                table: "PaymentReconciliationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_EnrollmentId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_EnrollmentRequestId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                column: "EnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_OpenSessionRequestId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                column: "OpenSessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_PayloadHash",
                schema: "payment",
                table: "PaymentTransactionEvents",
                column: "PayloadHash");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_PaymentId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_PaymentProvider_ProviderEventId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                columns: new[] { "PaymentProvider", "ProviderEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_PaymentProvider_ProviderInvoiceId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                columns: new[] { "PaymentProvider", "ProviderInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_PaymentProvider_ProviderPaymentId",
                schema: "payment",
                table: "PaymentTransactionEvents",
                columns: new[] { "PaymentProvider", "ProviderPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionEvents_ReceivedAt",
                schema: "payment",
                table: "PaymentTransactionEvents",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentReconciliationRuns",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "PaymentTransactionEvents",
                schema: "payment");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentProvider_ProviderInvoiceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentProvider_ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderInvoiceId",
                table: "Payments");
        }
    }
}
