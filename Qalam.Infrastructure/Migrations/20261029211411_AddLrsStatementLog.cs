using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLrsStatementLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lrs");

            migrationBuilder.CreateTable(
                name: "LrsStatementLogs",
                schema: "lrs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Verb = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ObjectIri = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceEntityId = table.Column<int>(type: "int", nullable: false),
                    StatementJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LrsStatementLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LrsStatementLogs_CreatedAt",
                schema: "lrs",
                table: "LrsStatementLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LrsStatementLogs_SourceEntityType_SourceEntityId",
                schema: "lrs",
                table: "LrsStatementLogs",
                columns: new[] { "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_LrsStatementLogs_StatementId",
                schema: "lrs",
                table: "LrsStatementLogs",
                column: "StatementId");

            migrationBuilder.CreateIndex(
                name: "IX_LrsStatementLogs_Status",
                schema: "lrs",
                table: "LrsStatementLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LrsStatementLogs",
                schema: "lrs");
        }
    }
}
