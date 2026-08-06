using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherTeachingPreferencesAndProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Teachers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OffersGroup",
                table: "Teachers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OffersInPerson",
                table: "Teachers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OffersIndividual",
                table: "Teachers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "OffersOnline",
                table: "Teachers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Teachers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "OffersGroup",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "OffersInPerson",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "OffersIndividual",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "OffersOnline",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Teachers");
        }
    }
}
