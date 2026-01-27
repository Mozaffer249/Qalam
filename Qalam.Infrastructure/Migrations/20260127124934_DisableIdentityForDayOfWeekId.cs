using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DisableIdentityForDayOfWeekId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "DaysOfWeek",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "IX_DaysOfWeek_IsActive",
                table: "DaysOfWeek",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DaysOfWeek_OrderIndex",
                table: "DaysOfWeek",
                column: "OrderIndex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DaysOfWeek_IsActive",
                table: "DaysOfWeek");

            migrationBuilder.DropIndex(
                name: "IX_DaysOfWeek_OrderIndex",
                table: "DaysOfWeek");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "DaysOfWeek",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
