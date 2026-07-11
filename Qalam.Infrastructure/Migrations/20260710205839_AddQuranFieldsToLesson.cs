using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranFieldsToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_UnitId_NameEn",
                schema: "education",
                table: "Lessons");

            migrationBuilder.AddColumn<int>(
                name: "QuranContentTypeId",
                schema: "education",
                table: "Lessons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuranLevelId",
                schema: "education",
                table: "Lessons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_QuranContentTypeId",
                schema: "education",
                table: "Lessons",
                column: "QuranContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_QuranLevelId",
                schema: "education",
                table: "Lessons",
                column: "QuranLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_QuranContentTypeId_QuranLevelId_NameEn",
                schema: "education",
                table: "Lessons",
                columns: new[] { "UnitId", "QuranContentTypeId", "QuranLevelId", "NameEn" },
                unique: true,
                filter: "[QuranContentTypeId] IS NOT NULL AND [QuranLevelId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_QuranContentTypes_QuranContentTypeId",
                schema: "education",
                table: "Lessons",
                column: "QuranContentTypeId",
                principalSchema: "quran",
                principalTable: "QuranContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_QuranLevels_QuranLevelId",
                schema: "education",
                table: "Lessons",
                column: "QuranLevelId",
                principalSchema: "quran",
                principalTable: "QuranLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_NameEn",
                schema: "education",
                table: "Lessons",
                columns: new[] { "UnitId", "NameEn" },
                unique: true,
                filter: "[QuranContentTypeId] IS NULL AND [QuranLevelId] IS NULL");

            migrationBuilder.Sql("""
                UPDATE er
                SET er.HasLessons = 1
                FROM teaching.EducationRules er
                INNER JOIN education.EducationDomains d ON d.Id = er.DomainId
                WHERE d.Code = 'quran'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_QuranContentTypes_QuranContentTypeId",
                schema: "education",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_QuranLevels_QuranLevelId",
                schema: "education",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_QuranContentTypeId",
                schema: "education",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_QuranLevelId",
                schema: "education",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_UnitId_QuranContentTypeId_QuranLevelId_NameEn",
                schema: "education",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "QuranContentTypeId",
                schema: "education",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "QuranLevelId",
                schema: "education",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_NameEn",
                schema: "education",
                table: "Lessons",
                columns: new[] { "UnitId", "NameEn" },
                unique: true);
        }
    }
}
