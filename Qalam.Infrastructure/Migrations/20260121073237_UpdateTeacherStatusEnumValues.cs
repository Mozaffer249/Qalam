using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeacherStatusEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing TeacherStatus values due to enum restructuring
            // Old enum: Pending=1, Active=2, Blocked=3, PendingVerification=4
            // New enum: AwaitingDocuments=1, PendingVerification=2, Active=3, Blocked=4

            // Update PendingVerification from 4 to 2
            migrationBuilder.Sql("UPDATE Teachers SET Status = 2 WHERE Status = 4");

            // Update Blocked from 3 to 4
            migrationBuilder.Sql("UPDATE Teachers SET Status = 4 WHERE Status = 3");

            // Update Active from 2 to 3
            migrationBuilder.Sql("UPDATE Teachers SET Status = 3 WHERE Status = 2");

            // Note: Pending (1) becomes AwaitingDocuments (1) - no change needed
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to old enum values
            // Revert Active from 3 to 2
            migrationBuilder.Sql("UPDATE Teachers SET Status = 2 WHERE Status = 3");

            // Revert Blocked from 4 to 3
            migrationBuilder.Sql("UPDATE Teachers SET Status = 3 WHERE Status = 4");

            // Revert PendingVerification from 2 to 4
            migrationBuilder.Sql("UPDATE Teachers SET Status = 4 WHERE Status = 2");

            // Note: AwaitingDocuments (1) becomes Pending (1) - no change needed
        }
    }
}
