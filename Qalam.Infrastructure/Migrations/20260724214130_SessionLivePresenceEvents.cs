using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionLivePresenceEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TeacherInRoom",
                schema: "course",
                table: "CourseSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeacherLeftAt",
                schema: "course",
                table: "CourseSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionLivePresenceEvents",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LiveKitEventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Identity = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLivePresenceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLivePresenceEvents_CourseSchedules_CourseScheduleId",
                        column: x => x.CourseScheduleId,
                        principalSchema: "course",
                        principalTable: "CourseSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLivePresenceEvents_CourseScheduleId_OccurredAt",
                schema: "course",
                table: "SessionLivePresenceEvents",
                columns: new[] { "CourseScheduleId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLivePresenceEvents_CourseScheduleId_Role_ParticipantId",
                schema: "course",
                table: "SessionLivePresenceEvents",
                columns: new[] { "CourseScheduleId", "Role", "ParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLivePresenceEvents_LiveKitEventId",
                schema: "course",
                table: "SessionLivePresenceEvents",
                column: "LiveKitEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLivePresenceEvents",
                schema: "course");

            migrationBuilder.DropColumn(
                name: "TeacherInRoom",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "TeacherLeftAt",
                schema: "course",
                table: "CourseSchedules");
        }
    }
}
