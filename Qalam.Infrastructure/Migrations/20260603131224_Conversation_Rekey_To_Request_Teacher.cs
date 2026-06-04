using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Conversation_Rekey_To_Request_Teacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferConversations_SessionOfferId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.AlterColumn<int>(
                name: "SessionOfferId",
                schema: "sr",
                table: "OfferConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "SessionRequestId",
                schema: "sr",
                table: "OfferConversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                schema: "sr",
                table: "OfferConversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_SessionOfferId",
                schema: "sr",
                table: "OfferConversations",
                column: "SessionOfferId",
                unique: true,
                filter: "[SessionOfferId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_SessionRequestId_TeacherId",
                schema: "sr",
                table: "OfferConversations",
                columns: new[] { "SessionRequestId", "TeacherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_TeacherId",
                schema: "sr",
                table: "OfferConversations",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_OfferConversations_SessionRequests_SessionRequestId",
                schema: "sr",
                table: "OfferConversations",
                column: "SessionRequestId",
                principalSchema: "sr",
                principalTable: "SessionRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OfferConversations_Teachers_TeacherId",
                schema: "sr",
                table: "OfferConversations",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfferConversations_SessionRequests_SessionRequestId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_OfferConversations_Teachers_TeacherId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropIndex(
                name: "IX_OfferConversations_SessionOfferId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropIndex(
                name: "IX_OfferConversations_SessionRequestId_TeacherId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropIndex(
                name: "IX_OfferConversations_TeacherId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropColumn(
                name: "SessionRequestId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                schema: "sr",
                table: "OfferConversations");

            migrationBuilder.AlterColumn<int>(
                name: "SessionOfferId",
                schema: "sr",
                table: "OfferConversations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferConversations_SessionOfferId",
                schema: "sr",
                table: "OfferConversations",
                column: "SessionOfferId",
                unique: true);
        }
    }
}
