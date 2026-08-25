using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenSessionRequestPricingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PricingSnapshotId",
                schema: "sr",
                table: "SessionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_PricingSnapshotId",
                schema: "sr",
                table: "SessionRequests",
                column: "PricingSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionRequests_PricingSnapshots_PricingSnapshotId",
                schema: "sr",
                table: "SessionRequests",
                column: "PricingSnapshotId",
                principalSchema: "pricing",
                principalTable: "PricingSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_PricingSnapshots_PricingSnapshotId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropIndex(
                name: "IX_SessionRequests_PricingSnapshotId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropColumn(
                name: "PricingSnapshotId",
                schema: "sr",
                table: "SessionRequests");
        }
    }
}
