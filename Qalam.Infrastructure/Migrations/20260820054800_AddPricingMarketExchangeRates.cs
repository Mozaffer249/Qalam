using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingMarketExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateFromBase",
                schema: "pricing",
                table: "PricingMarkets",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql("""
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 1.0 WHERE Code = 'sa';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 1.0 WHERE Code = 'ae';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 1.0 WHERE Code = 'qa';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 0.08 WHERE Code = 'kw';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 0.10 WHERE Code = 'bh';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 0.10 WHERE Code = 'om';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 8.0 WHERE Code = 'eg';
                UPDATE pricing.PricingMarkets SET ExchangeRateFromBase = 0.19 WHERE Code = 'jo';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRateFromBase",
                schema: "pricing",
                table: "PricingMarkets");
        }
    }
}
