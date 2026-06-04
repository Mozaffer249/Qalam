using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenSessionRequestTargetedTeacherId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetedTeacherId",
                schema: "sr",
                table: "SessionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_TargetedTeacherId",
                schema: "sr",
                table: "SessionRequests",
                column: "TargetedTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionRequests_Teachers_TargetedTeacherId",
                schema: "sr",
                table: "SessionRequests",
                column: "TargetedTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Teachers_TargetedTeacherId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropIndex(
                name: "IX_SessionRequests_TargetedTeacherId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropColumn(
                name: "TargetedTeacherId",
                schema: "sr",
                table: "SessionRequests");
        }
    }
}
