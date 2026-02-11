using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCourseDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_StartDate_EndDate",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "course",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                schema: "course",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                schema: "course",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_StartDate_EndDate",
                schema: "course",
                table: "Courses",
                columns: new[] { "StartDate", "EndDate" });
        }
    }
}
