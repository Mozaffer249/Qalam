using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitTeacherDomainCustomPricesBySessionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReflectCustomPriceToStudent",
                schema: "teacher",
                table: "TeacherDomainPricings",
                newName: "ReflectCustomIndividualPriceToStudent");

            migrationBuilder.RenameColumn(
                name: "CustomPricePerHour",
                schema: "teacher",
                table: "TeacherDomainPricings",
                newName: "CustomIndividualPricePerHour");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomGroupPricePerHour",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReflectCustomGroupPriceToStudent",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE teacher.TeacherDomainPricings
                SET CustomGroupPricePerHour = CustomIndividualPricePerHour,
                    ReflectCustomGroupPriceToStudent = ReflectCustomIndividualPriceToStudent
                WHERE CustomIndividualPricePerHour IS NOT NULL
                   OR ReflectCustomIndividualPriceToStudent = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomGroupPricePerHour",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.DropColumn(
                name: "ReflectCustomGroupPriceToStudent",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.RenameColumn(
                name: "ReflectCustomIndividualPriceToStudent",
                schema: "teacher",
                table: "TeacherDomainPricings",
                newName: "ReflectCustomPriceToStudent");

            migrationBuilder.RenameColumn(
                name: "CustomIndividualPricePerHour",
                schema: "teacher",
                table: "TeacherDomainPricings",
                newName: "CustomPricePerHour");
        }
    }
}
