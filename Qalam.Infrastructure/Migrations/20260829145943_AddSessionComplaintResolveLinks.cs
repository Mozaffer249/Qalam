using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionComplaintResolveLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefundId",
                schema: "course",
                table: "SessionComplaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplacementScheduleId",
                schema: "course",
                table: "SessionComplaints",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundId",
                schema: "course",
                table: "SessionComplaints");

            migrationBuilder.DropColumn(
                name: "ReplacementScheduleId",
                schema: "course",
                table: "SessionComplaints");
        }
    }
}
