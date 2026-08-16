using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitSkillsDomainsAndWritableFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentSubjectId",
                schema: "education",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EducationLevelAfterSubject",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasParentSubject",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasWritableFilters",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WritableFilterSlots",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AfterStep = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiredWhenSubjectCodeContains = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritableFilterSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritableFilterSlots_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WritableFilterValues",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSeeded = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritableFilterValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritableFilterValues_WritableFilterSlots_SlotId",
                        column: x => x.SlotId,
                        principalSchema: "education",
                        principalTable: "WritableFilterSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjectWritableFilters",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherSubjectId = table.Column<int>(type: "int", nullable: false),
                    WritableFilterValueId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectWritableFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectWritableFilters_TeacherSubjects_TeacherSubjectId",
                        column: x => x.TeacherSubjectId,
                        principalSchema: "education",
                        principalTable: "TeacherSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectWritableFilters_WritableFilterValues_WritableFilterValueId",
                        column: x => x.WritableFilterValueId,
                        principalSchema: "education",
                        principalTable: "WritableFilterValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ParentSubjectId",
                schema: "education",
                table: "Subjects",
                column: "ParentSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectWritableFilters_TeacherSubjectId_WritableFilterValueId",
                schema: "teacher",
                table: "TeacherSubjectWritableFilters",
                columns: new[] { "TeacherSubjectId", "WritableFilterValueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectWritableFilters_WritableFilterValueId",
                schema: "teacher",
                table: "TeacherSubjectWritableFilters",
                column: "WritableFilterValueId");

            migrationBuilder.CreateIndex(
                name: "IX_WritableFilterSlots_DomainId_Code",
                schema: "education",
                table: "WritableFilterSlots",
                columns: new[] { "DomainId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritableFilterSlots_DomainId_OrderIndex",
                schema: "education",
                table: "WritableFilterSlots",
                columns: new[] { "DomainId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_WritableFilterValues_IsActive",
                schema: "education",
                table: "WritableFilterValues",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WritableFilterValues_SlotId_Code",
                schema: "education",
                table: "WritableFilterValues",
                columns: new[] { "SlotId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WritableFilterValues_SlotId_NormalizedText",
                schema: "education",
                table: "WritableFilterValues",
                columns: new[] { "SlotId", "NormalizedText" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Subjects_ParentSubjectId",
                schema: "education",
                table: "Subjects",
                column: "ParentSubjectId",
                principalSchema: "education",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Subjects_ParentSubjectId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "TeacherSubjectWritableFilters",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "WritableFilterValues",
                schema: "education");

            migrationBuilder.DropTable(
                name: "WritableFilterSlots",
                schema: "education");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_ParentSubjectId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ParentSubjectId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "EducationLevelAfterSubject",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "HasParentSubject",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "HasWritableFilters",
                schema: "teaching",
                table: "EducationRules");
        }
    }
}
