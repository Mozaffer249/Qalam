using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainIdToCurriculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add DomainId column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "DomainId",
                schema: "education",
                table: "Curriculums",
                type: "int",
                nullable: true);

            // Step 2: Data Migration - Set all existing Curriculums to School Domain (Id = 1)
            migrationBuilder.Sql(@"
                UPDATE education.Curriculums 
                SET DomainId = 1 
                WHERE DomainId IS NULL;
            ");

            // Step 3: Make DomainId required (NOT NULL)
            migrationBuilder.AlterColumn<int>(
                name: "DomainId",
                schema: "education",
                table: "Curriculums",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Step 4: Create index
            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_DomainId_Country",
                schema: "education",
                table: "Curriculums",
                columns: new[] { "DomainId", "Country" });

            // Step 5: Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_EducationDomains_DomainId",
                schema: "education",
                table: "Curriculums",
                column: "DomainId",
                principalSchema: "education",
                principalTable: "EducationDomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_EducationDomains_DomainId",
                schema: "education",
                table: "Curriculums");

            migrationBuilder.DropIndex(
                name: "IX_Curriculums_DomainId_Country",
                schema: "education",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "DomainId",
                schema: "education",
                table: "Curriculums");
        }
    }
}
