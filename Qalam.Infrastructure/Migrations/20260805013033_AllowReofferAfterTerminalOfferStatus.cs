using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowReofferAfterTerminalOfferStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionOffers_SessionRequestId_TeacherId",
                schema: "sr",
                table: "SessionOffers");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_SessionRequestId_TeacherId",
                schema: "sr",
                table: "SessionOffers",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true,
                filter: "[Status] IN (1,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionOffers_SessionRequestId_TeacherId",
                schema: "sr",
                table: "SessionOffers");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOffers_SessionRequestId_TeacherId",
                schema: "sr",
                table: "SessionOffers",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true,
                filter: "[Status] <> 5");
        }
    }
}
