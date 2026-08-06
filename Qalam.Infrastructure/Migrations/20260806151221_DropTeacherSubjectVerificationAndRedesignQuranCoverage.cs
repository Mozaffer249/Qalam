using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTeacherSubjectVerificationAndRedesignQuranCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- 1. Create new tables / columns first (needed for backfill) ---
            migrationBuilder.AddColumn<int>(
                name: "QuranContentTypeId",
                schema: "course",
                table: "CourseSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuranLevelId",
                schema: "course",
                table: "CourseSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeacherSubjectQuranContentTypes",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherSubjectId = table.Column<int>(type: "int", nullable: false),
                    QuranContentTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectQuranContentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectQuranContentTypes_QuranContentTypes_QuranContentTypeId",
                        column: x => x.QuranContentTypeId,
                        principalSchema: "quran",
                        principalTable: "QuranContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectQuranContentTypes_TeacherSubjects_TeacherSubjectId",
                        column: x => x.TeacherSubjectId,
                        principalSchema: "education",
                        principalTable: "TeacherSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjectQuranLevels",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherSubjectId = table.Column<int>(type: "int", nullable: false),
                    QuranLevelId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectQuranLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectQuranLevels_QuranLevels_QuranLevelId",
                        column: x => x.QuranLevelId,
                        principalSchema: "quran",
                        principalTable: "QuranLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectQuranLevels_TeacherSubjects_TeacherSubjectId",
                        column: x => x.TeacherSubjectId,
                        principalSchema: "education",
                        principalTable: "TeacherSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // --- 2. Merge duplicate (TeacherId, SubjectId) rows onto lowest Id ---
            // CTEs only apply to the next statement in SQL Server; materialize into temp tables
            // so survivors/dupes can be reused across UPDATE/DELETE/repoint steps.
            migrationBuilder.Sql(@"
IF OBJECT_ID('tempdb..#survivors') IS NOT NULL DROP TABLE #survivors;
IF OBJECT_ID('tempdb..#dupes') IS NOT NULL DROP TABLE #dupes;

-- Survivor = lowest Id per (TeacherId, SubjectId)
SELECT TeacherId, SubjectId, MIN(Id) AS SurvivorId
INTO #survivors
FROM education.TeacherSubjects
GROUP BY TeacherId, SubjectId
HAVING COUNT(*) > 1;

SELECT ts.Id AS DupeId, s.SurvivorId
INTO #dupes
FROM education.TeacherSubjects ts
INNER JOIN #survivors s ON ts.TeacherId = s.TeacherId AND ts.SubjectId = s.SubjectId
WHERE ts.Id <> s.SurvivorId;

-- Activate survivor if any row in the group is active / FULL
UPDATE ts
SET IsActive = 1,
    CanTeachFullSubject = CASE
        WHEN EXISTS (
            SELECT 1 FROM education.TeacherSubjects x
            WHERE x.TeacherId = ts.TeacherId AND x.SubjectId = ts.SubjectId AND x.CanTeachFullSubject = 1
        ) THEN 1 ELSE ts.CanTeachFullSubject END
FROM education.TeacherSubjects ts
INNER JOIN #survivors s ON ts.Id = s.SurvivorId;

-- If survivor is FULL, drop its unit rows (FULL = all catalog units)
DELETE tsu
FROM teacher.TeacherSubjectUnits tsu
INNER JOIN education.TeacherSubjects ts ON tsu.TeacherSubjectId = ts.Id
INNER JOIN #survivors s ON ts.Id = s.SurvivorId
WHERE ts.CanTeachFullSubject = 1;

-- Delete dupe units that would collide with survivor on (UnitId, Type, Level)
DELETE tsu
FROM teacher.TeacherSubjectUnits tsu
INNER JOIN #dupes d ON tsu.TeacherSubjectId = d.DupeId
INNER JOIN education.TeacherSubjects survivor ON survivor.Id = d.SurvivorId
WHERE survivor.CanTeachFullSubject = 0
  AND EXISTS (
      SELECT 1 FROM teacher.TeacherSubjectUnits existing
      WHERE existing.TeacherSubjectId = d.SurvivorId
        AND existing.UnitId = tsu.UnitId
        AND ISNULL(existing.QuranContentTypeId, -1) = ISNULL(tsu.QuranContentTypeId, -1)
        AND ISNULL(existing.QuranLevelId, -1) = ISNULL(tsu.QuranLevelId, -1)
  );

-- Repoint remaining units from dupes to survivor (only when survivor is not FULL)
UPDATE tsu
SET TeacherSubjectId = d.SurvivorId
FROM teacher.TeacherSubjectUnits tsu
INNER JOIN #dupes d ON tsu.TeacherSubjectId = d.DupeId
INNER JOIN education.TeacherSubjects survivor ON survivor.Id = d.SurvivorId
WHERE survivor.CanTeachFullSubject = 0;

-- Delete units that belonged to FULL survivors' dupes (already covered by FULL)
DELETE tsu
FROM teacher.TeacherSubjectUnits tsu
INNER JOIN #dupes d ON tsu.TeacherSubjectId = d.DupeId
INNER JOIN education.TeacherSubjects survivor ON survivor.Id = d.SurvivorId
WHERE survivor.CanTeachFullSubject = 1;

-- Repoint courses from dupe TeacherSubject rows to survivor
UPDATE c
SET TeacherSubjectId = d.SurvivorId
FROM course.Courses c
INNER JOIN #dupes d ON c.TeacherSubjectId = d.DupeId;

-- Delete losing TeacherSubject rows
DELETE ts
FROM education.TeacherSubjects ts
INNER JOIN #dupes d ON ts.Id = d.DupeId;

DROP TABLE #dupes;
DROP TABLE #survivors;
");

            // --- 3. Backfill Quran coverage sets from unit rows (before dropping columns) ---
            // Empty set = all. If any unit row has NULL type for a subject, skip inserting types (means all).
            migrationBuilder.Sql(@"
INSERT INTO teacher.TeacherSubjectQuranContentTypes (TeacherSubjectId, QuranContentTypeId, CreatedAt)
SELECT DISTINCT tsu.TeacherSubjectId, tsu.QuranContentTypeId, SYSUTCDATETIME()
FROM teacher.TeacherSubjectUnits tsu
WHERE tsu.QuranContentTypeId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM teacher.TeacherSubjectUnits x
      WHERE x.TeacherSubjectId = tsu.TeacherSubjectId AND x.QuranContentTypeId IS NULL
  );

INSERT INTO teacher.TeacherSubjectQuranLevels (TeacherSubjectId, QuranLevelId, CreatedAt)
SELECT DISTINCT tsu.TeacherSubjectId, tsu.QuranLevelId, SYSUTCDATETIME()
FROM teacher.TeacherSubjectUnits tsu
WHERE tsu.QuranLevelId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM teacher.TeacherSubjectUnits x
      WHERE x.TeacherSubjectId = tsu.TeacherSubjectId AND x.QuranLevelId IS NULL
  );
");

            // --- 4. Collapse TeacherSubjectUnits to distinct (TeacherSubjectId, UnitId) ---
            migrationBuilder.Sql(@"
;WITH ranked AS (
    SELECT Id,
           ROW_NUMBER() OVER (PARTITION BY TeacherSubjectId, UnitId ORDER BY Id) AS rn
    FROM teacher.TeacherSubjectUnits
)
DELETE FROM teacher.TeacherSubjectUnits
WHERE Id IN (SELECT Id FROM ranked WHERE rn > 1);
");

            // --- 5. Drop old FKs / indexes / columns ---
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
                name: "IX_TeacherSubjects_TeacherId_SubjectId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropColumn(
                name: "QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "RejectionSource",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                schema: "education",
                table: "TeacherSubjects");

            // --- 6. New unique indexes + FKs ---
            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                columns: new[] { "TeacherSubjectId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId_SubjectId",
                schema: "education",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_QuranContentTypeId",
                schema: "course",
                table: "CourseSessions",
                column: "QuranContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_QuranLevelId",
                schema: "course",
                table: "CourseSessions",
                column: "QuranLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectQuranContentTypes_QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectQuranContentTypes",
                column: "QuranContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectQuranContentTypes_TeacherSubjectId_QuranContentTypeId",
                schema: "teacher",
                table: "TeacherSubjectQuranContentTypes",
                columns: new[] { "TeacherSubjectId", "QuranContentTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectQuranLevels_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectQuranLevels",
                column: "QuranLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectQuranLevels_TeacherSubjectId_QuranLevelId",
                schema: "teacher",
                table: "TeacherSubjectQuranLevels",
                columns: new[] { "TeacherSubjectId", "QuranLevelId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSessions_QuranContentTypes_QuranContentTypeId",
                schema: "course",
                table: "CourseSessions",
                column: "QuranContentTypeId",
                principalSchema: "quran",
                principalTable: "QuranContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSessions_QuranLevels_QuranLevelId",
                schema: "course",
                table: "CourseSessions",
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
                name: "FK_CourseSessions_QuranContentTypes_QuranContentTypeId",
                schema: "course",
                table: "CourseSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseSessions_QuranLevels_QuranLevelId",
                schema: "course",
                table: "CourseSessions");

            migrationBuilder.DropTable(
                name: "TeacherSubjectQuranContentTypes",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "TeacherSubjectQuranLevels",
                schema: "teacher");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId",
                schema: "teacher",
                table: "TeacherSubjectUnits");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_TeacherId_SubjectId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_QuranContentTypeId",
                schema: "course",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_QuranLevelId",
                schema: "course",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "QuranContentTypeId",
                schema: "course",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "QuranLevelId",
                schema: "course",
                table: "CourseSessions");

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

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "education",
                table: "TeacherSubjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionSource",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "education",
                table: "TeacherSubjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByAdminId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: false,
                defaultValue: 2);

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
                name: "IX_TeacherSubjects_TeacherId_SubjectId",
                schema: "education",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId" });

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
    }
}
