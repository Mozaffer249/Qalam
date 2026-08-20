using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingMarkets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainSessionPrices_DomainId_SessionTypeCode_EffectiveFrom",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.DropIndex(
                name: "IX_DomainSessionPrices_DomainId_SessionTypeCode_EffectiveTo",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.AddColumn<string>(
                name: "PreferredMarketCode",
                schema: "security",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "pricing",
                table: "PricingSnapshots",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MarketCode",
                schema: "pricing",
                table: "PricingSnapshots",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MarketCode",
                schema: "pricing",
                table: "DomainSessionPrices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PricingMarkets",
                schema: "pricing",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingMarkets", x => x.Code);
                });

            migrationBuilder.Sql("""
                INSERT INTO pricing.PricingMarkets (Code, NameEn, NameAr, Currency, IsActive, IsDefault, CreatedAt)
                VALUES
                    ('sa', 'Saudi Arabia', N'المملكة العربية السعودية', 'SAR', 1, 1, SYSUTCDATETIME()),
                    ('ae', 'United Arab Emirates', N'الإمارات العربية المتحدة', 'AED', 1, 0, SYSUTCDATETIME()),
                    ('kw', 'Kuwait', N'الكويت', 'KWD', 1, 0, SYSUTCDATETIME()),
                    ('qa', 'Qatar', N'قطر', 'QAR', 1, 0, SYSUTCDATETIME()),
                    ('bh', 'Bahrain', N'البحرين', 'BHD', 1, 0, SYSUTCDATETIME()),
                    ('om', 'Oman', N'عُمان', 'OMR', 1, 0, SYSUTCDATETIME()),
                    ('eg', 'Egypt', N'مصر', 'EGP', 1, 0, SYSUTCDATETIME()),
                    ('jo', 'Jordan', N'الأردن', 'JOD', 1, 0, SYSUTCDATETIME());
                """);

            migrationBuilder.Sql("""
                UPDATE pricing.DomainSessionPrices SET MarketCode = 'sa' WHERE MarketCode = '' OR MarketCode IS NULL;
                UPDATE pricing.PricingSnapshots SET MarketCode = 'sa', Currency = 'SAR' WHERE MarketCode = '' OR MarketCode IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PreferredMarketCode",
                schema: "security",
                table: "Users",
                column: "PreferredMarketCode");

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_DomainId",
                schema: "pricing",
                table: "DomainSessionPrices",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_MarketCode_DomainId_SessionTypeCode_EffectiveFrom",
                schema: "pricing",
                table: "DomainSessionPrices",
                columns: new[] { "MarketCode", "DomainId", "SessionTypeCode", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_MarketCode_DomainId_SessionTypeCode_EffectiveTo",
                schema: "pricing",
                table: "DomainSessionPrices",
                columns: new[] { "MarketCode", "DomainId", "SessionTypeCode", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingMarkets_IsDefault",
                schema: "pricing",
                table: "PricingMarkets",
                column: "IsDefault");

            migrationBuilder.AddForeignKey(
                name: "FK_DomainSessionPrices_PricingMarkets_MarketCode",
                schema: "pricing",
                table: "DomainSessionPrices",
                column: "MarketCode",
                principalSchema: "pricing",
                principalTable: "PricingMarkets",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PricingMarkets_PreferredMarketCode",
                schema: "security",
                table: "Users",
                column: "PreferredMarketCode",
                principalSchema: "pricing",
                principalTable: "PricingMarkets",
                principalColumn: "Code",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DomainSessionPrices_PricingMarkets_MarketCode",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_PricingMarkets_PreferredMarketCode",
                schema: "security",
                table: "Users");

            migrationBuilder.DropTable(
                name: "PricingMarkets",
                schema: "pricing");

            migrationBuilder.DropIndex(
                name: "IX_Users_PreferredMarketCode",
                schema: "security",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_DomainSessionPrices_DomainId",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.DropIndex(
                name: "IX_DomainSessionPrices_MarketCode_DomainId_SessionTypeCode_EffectiveFrom",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.DropIndex(
                name: "IX_DomainSessionPrices_MarketCode_DomainId_SessionTypeCode_EffectiveTo",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.DropColumn(
                name: "PreferredMarketCode",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "pricing",
                table: "PricingSnapshots");

            migrationBuilder.DropColumn(
                name: "MarketCode",
                schema: "pricing",
                table: "PricingSnapshots");

            migrationBuilder.DropColumn(
                name: "MarketCode",
                schema: "pricing",
                table: "DomainSessionPrices");

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_DomainId_SessionTypeCode_EffectiveFrom",
                schema: "pricing",
                table: "DomainSessionPrices",
                columns: new[] { "DomainId", "SessionTypeCode", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainSessionPrices_DomainId_SessionTypeCode_EffectiveTo",
                schema: "pricing",
                table: "DomainSessionPrices",
                columns: new[] { "DomainId", "SessionTypeCode", "EffectiveTo" });
        }
    }
}
