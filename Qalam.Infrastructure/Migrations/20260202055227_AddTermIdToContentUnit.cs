using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermIdToContentUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.AddColumn<int>(
                name: "QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TermId",
                schema: "education",
                table: "ContentUnits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                column: "QuranContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                column: "QuranLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId_QuranContentTypeId_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                columns: new[] { "TeacherSubjectId", "UnitId", "QuranContentTypeId", "QuranLevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_UnitId_QuranContentTypeId_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                columns: new[] { "UnitId", "QuranContentTypeId", "QuranLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentUnits_TermId",
                schema: "education",
                table: "ContentUnits",
                column: "TermId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentUnits_AcademicTerms_TermId",
                schema: "education",
                table: "ContentUnits",
                column: "TermId",
                principalSchema: "education",
                principalTable: "AcademicTerms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjectUnits_QuranContentTypes_QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                column: "QuranContentTypeId",
                principalSchema: "quran",
                principalTable: "QuranContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjectUnits_QuranLevels_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                column: "QuranLevelId",
                principalSchema: "quran",
                principalTable: "QuranLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentUnits_AcademicTerms_TermId",
                schema: "education",
                table: "ContentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjectUnits_QuranContentTypes_QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjectUnits_QuranLevels_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjectUnits_QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjectUnits_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId_QuranContentTypeId_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjectUnits_UnitId_QuranContentTypeId_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropIndex(
                name: "IX_ContentUnits_TermId",
                schema: "education",
                table: "ContentUnits");

            migrationBuilder.DropColumn(
                name: "QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropColumn(
                name: "QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropColumn(
                name: "TermId",
                schema: "education",
                table: "ContentUnits");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                columns: new[] { "TeacherSubjectId", "UnitId" },
                unique: true);
        }
    }
}
