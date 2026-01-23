using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "TeacherDocuments");

            migrationBuilder.RenameColumn(
                name: "VerifiedByAdminId",
                table: "TeacherDocuments",
                newName: "ReviewedByAdminId");

            migrationBuilder.RenameColumn(
                name: "VerifiedAt",
                table: "TeacherDocuments",
                newName: "ReviewedAt");

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "TeacherDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "TeacherDocuments");

            migrationBuilder.RenameColumn(
                name: "ReviewedByAdminId",
                table: "TeacherDocuments",
                newName: "VerifiedByAdminId");

            migrationBuilder.RenameColumn(
                name: "ReviewedAt",
                table: "TeacherDocuments",
                newName: "VerifiedAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "TeacherDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
