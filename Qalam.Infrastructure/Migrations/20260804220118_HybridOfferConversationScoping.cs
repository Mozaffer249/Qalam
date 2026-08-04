using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HybridOfferConversationScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferConversations_SessionRequestId_TeacherId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.AddColumn<bool>(
                name: "IsOfferScoped",
                schema: "sr",
                table: "OfferConversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_SessionRequestId_TeacherId",
                schema: "sr",
                table: "OfferConversations",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true,
                filter: "[IsOfferScoped] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferConversations_SessionRequestId_TeacherId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropColumn(
                name: "IsOfferScoped",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_SessionRequestId_TeacherId",
                schema: "sr",
                table: "OfferConversations",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true);
        }
    }
}
