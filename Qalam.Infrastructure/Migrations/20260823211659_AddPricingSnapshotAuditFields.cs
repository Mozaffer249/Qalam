using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingSnapshotAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EarningsPricePerHour",
                schema: "pricing",
                table: "PricingSnapshots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReflectCustomPriceToStudent",
                schema: "pricing",
                table: "PricingSnapshots",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarningsPricePerHour",
                schema: "pricing",
                table: "PricingSnapshots");

            migrationBuilder.DropColumn(
                name: "ReflectCustomPriceToStudent",
                schema: "pricing",
                table: "PricingSnapshots");
        }
    }
}
