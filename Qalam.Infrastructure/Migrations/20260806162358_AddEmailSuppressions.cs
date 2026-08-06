using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSuppressions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSuppressions",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Diagnostic = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BounceCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastBounceAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSuppressions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_Email",
                schema: "messaging",
                table: "EmailSuppressions",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_LastBounceAt",
                schema: "messaging",
                table: "EmailSuppressions",
                column: "LastBounceAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSuppressions",
                schema: "messaging");
        }
    }
}
