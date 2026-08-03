using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SlimOpenSessionRequestStatusPaidTerminal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap removed OSR statuses Scheduled(8) / InProgress(9) / Completed(10) → Paid(7).
            migrationBuilder.Sql(
                """
                UPDATE [sr].[SessionRequests]
                SET [Status] = 7
                WHERE [Status] IN (8, 9, 10);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data remap — removed statuses are no longer in the enum.
        }
    }
}
