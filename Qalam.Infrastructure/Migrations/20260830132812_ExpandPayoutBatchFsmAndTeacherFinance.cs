using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPayoutBatchFsmAndTeacherFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "PayoutBatches",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "PayoutBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "PayoutBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "PayoutBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "PayoutBatches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "PayoutBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedByUserId",
                table: "PayoutBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "PayoutBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "PayoutBatches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Remap legacy Paid (3) to new Paid (4) after enum expansion.
            migrationBuilder.Sql("UPDATE PayoutBatches SET Status = 4 WHERE Status = 3;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE PayoutBatches SET Status = 3 WHERE Status = 4;");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PayoutBatches");
        }
    }
}
