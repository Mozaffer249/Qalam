using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEnrollmentRequestToUserLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollmentRequests_Students_RequestedByStudentId",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.RenameColumn(
                name: "RequestedByStudentId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                newName: "RequestedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseEnrollmentRequests_RequestedByStudentId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                newName: "IX_CourseEnrollmentRequests_RequestedByUserId");

            // Migrate existing data: convert StudentId to UserId
            migrationBuilder.Sql(@"
                UPDATE cer
                SET cer.RequestedByUserId = s.UserId
                FROM [course].[CourseEnrollmentRequests] cer
                INNER JOIN [student].[Students] s ON s.Id = cer.RequestedByUserId
            ");

            migrationBuilder.AlterColumn<int>(
                name: "InvitedByStudentId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MemberType",
                schema: "course",
                table: "CourseRequestGroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 2); // Default to Invited for existing rows

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollmentRequests_Users_RequestedByUserId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "RequestedByUserId",
                principalSchema: "security",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollmentRequests_Users_RequestedByUserId",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.DropColumn(
                name: "MemberType",
                schema: "course",
                table: "CourseRequestGroupMembers");

            migrationBuilder.RenameColumn(
                name: "RequestedByUserId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                newName: "RequestedByStudentId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseEnrollmentRequests_RequestedByUserId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                newName: "IX_CourseEnrollmentRequests_RequestedByStudentId");

            migrationBuilder.AlterColumn<int>(
                name: "InvitedByStudentId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollmentRequests_Students_RequestedByStudentId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "RequestedByStudentId",
                principalSchema: "student",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
