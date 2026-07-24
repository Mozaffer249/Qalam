using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnrollmentConversations",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    StudentUserId = table.Column<int>(type: "int", nullable: false),
                    StudentLastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeacherLastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentConversations_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentConversations_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentConversations_Users_StudentUserId",
                        column: x => x.StudentUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentConversationMessages",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentConversationMessages_EnrollmentConversations_EnrollmentConversationId",
                        column: x => x.EnrollmentConversationId,
                        principalSchema: "course",
                        principalTable: "EnrollmentConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentConversationMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentConversationMessages_EnrollmentConversationId_SentAt",
                schema: "course",
                table: "EnrollmentConversationMessages",
                columns: new[] { "EnrollmentConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentConversationMessages_SenderUserId",
                schema: "course",
                table: "EnrollmentConversationMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentConversations_EnrollmentId",
                schema: "course",
                table: "EnrollmentConversations",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentConversations_LastMessageAt",
                schema: "course",
                table: "EnrollmentConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentConversations_StudentUserId",
                schema: "course",
                table: "EnrollmentConversations",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentConversations_TeacherId",
                schema: "course",
                table: "EnrollmentConversations",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrollmentConversationMessages",
                schema: "course");

            migrationBuilder.DropTable(
                name: "EnrollmentConversations",
                schema: "course");
        }
    }
}
