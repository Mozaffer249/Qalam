using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStudentGuardianRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Users_TeacherId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.EnsureSchema(
                name: "course");

            migrationBuilder.EnsureSchema(
                name: "student");

            migrationBuilder.EnsureSchema(
                name: "session");

            migrationBuilder.EnsureSchema(
                name: "teacher");

            migrationBuilder.AddColumn<bool>(
                name: "CanTeachFullSubject",
                schema: "education",
                table: "TeacherSubjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "education",
                table: "TeacherSubjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurriculumId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "education",
                table: "TeacherSubjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LevelId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherId1",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "education",
                table: "TeacherSubjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DaysOfWeek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DaysOfWeek", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guardians_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayerUserId = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReceiptUrl = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    ReceiptPath = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Users_PayerUserId",
                        column: x => x.PayerUserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RatingAverage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjectUnits",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherSubjectId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectUnits_ContentUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectUnits_TeacherSubjects_TeacherSubjectId",
                        column: x => x.TeacherSubjectId,
                        principalSchema: "education",
                        principalTable: "TeacherSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsMinor = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    GuardianId = table.Column<int>(type: "int", nullable: true),
                    GuardianRelation = table.Column<int>(type: "int", nullable: true),
                    DomainId = table.Column<int>(type: "int", nullable: true),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    LevelId = table.Column<int>(type: "int", nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.CheckConstraint("CK_Students_Minor_RequiresGuardian", "([IsMinor] = 0) OR ([IsMinor] = 1 AND [GuardianId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Students_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "education",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Guardians_GuardianId",
                        column: x => x.GuardianId,
                        principalSchema: "student",
                        principalTable: "Guardians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentItems_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    LevelId = table.Column<int>(type: "int", nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    SessionTypeId = table.Column<int>(type: "int", nullable: false),
                    IsFlexible = table.Column<bool>(type: "bit", nullable: false),
                    SessionsCount = table.Column<int>(type: "int", nullable: true),
                    SessionDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxStudents = table.Column<int>(type: "int", nullable: true),
                    CanIncludeInPackages = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "education",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_SessionTypes_SessionTypeId",
                        column: x => x.SessionTypeId,
                        principalSchema: "teaching",
                        principalTable: "SessionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "education",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_TeachingModes_TeachingModeId",
                        column: x => x.TeachingModeId,
                        principalSchema: "teaching",
                        principalTable: "TeachingModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    MaxDistanceKm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAreas_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "common",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherAreas_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAuditLogs_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeekId = table.Column<int>(type: "int", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilities_DaysOfWeek_DayOfWeekId",
                        column: x => x.DayOfWeekId,
                        principalTable: "DaysOfWeek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilities_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilities_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalSchema: "common",
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAvailabilityExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    ExceptionType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAvailabilityExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilityExceptions_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherAvailabilityExceptions_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalSchema: "common",
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByAdminId = table.Column<int>(type: "int", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherDocuments_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequests",
                schema: "session",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    LevelId = table.Column<int>(type: "int", nullable: true),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    SessionTypeId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "common",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_SessionTypes_SessionTypeId",
                        column: x => x.SessionTypeId,
                        principalSchema: "teaching",
                        principalTable: "SessionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "education",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionRequests_TeachingModes_TeachingModeId",
                        column: x => x.TeachingModeId,
                        principalSchema: "teaching",
                        principalTable: "TeachingModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherReviews_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherReviews_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollmentRequests",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    RequestedByStudentId = table.Column<int>(type: "int", nullable: false),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentRequests_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentRequests_Students_RequestedByStudentId",
                        column: x => x.RequestedByStudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollments",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByTeacherId = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrollmentStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Teachers_ApprovedByTeacherId",
                        column: x => x.ApprovedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseSessions",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SessionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "course",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRequestOffers",
                schema: "session",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ProposedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProposedSchedule = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRequestOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRequestOffers_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "session",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionRequestOffers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "session",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionRequestId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_SessionRequests_SessionRequestId",
                        column: x => x.SessionRequestId,
                        principalSchema: "session",
                        principalTable: "SessionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessions_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseRequestGroupMembers",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentRequestId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    InvitedByStudentId = table.Column<int>(type: "int", nullable: false),
                    ConfirmationStatus = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequestGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequestGroupMembers_CourseEnrollmentRequests_CourseEnrollmentRequestId",
                        column: x => x.CourseEnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseRequestGroupMembers_Students_InvitedByStudentId",
                        column: x => x.InvitedByStudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseRequestGroupMembers_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseRequestSelectedAvailabilities",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentRequestId = table.Column<int>(type: "int", nullable: false),
                    TeacherAvailabilityId = table.Column<int>(type: "int", nullable: false),
                    PriorityOrder = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequestSelectedAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedAvailabilities_CourseEnrollmentRequests_CourseEnrollmentRequestId",
                        column: x => x.CourseEnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedAvailabilities_TeacherAvailabilities_TeacherAvailabilityId",
                        column: x => x.TeacherAvailabilityId,
                        principalTable: "TeacherAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollmentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollmentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentPayments_CourseEnrollments_CourseEnrollmentId",
                        column: x => x.CourseEnrollmentId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentPayments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseSchedules",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TeacherAvailabilityId = table.Column<int>(type: "int", nullable: false),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSchedules_CourseEnrollments_CourseEnrollmentId",
                        column: x => x.CourseEnrollmentId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSchedules_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "common",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSchedules_TeacherAvailabilities_TeacherAvailabilityId",
                        column: x => x.TeacherAvailabilityId,
                        principalTable: "TeacherAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSchedules_TeachingModes_TeachingModeId",
                        column: x => x.TeachingModeId,
                        principalSchema: "teaching",
                        principalTable: "TeachingModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledSessions",
                schema: "session",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "common",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "session",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_TeachingModes_TeachingModeId",
                        column: x => x.TeachingModeId,
                        principalSchema: "teaching",
                        principalTable: "TeachingModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalSchema: "common",
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_CurriculumId",
                schema: "education",
                table: "TeacherSubjects",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_GradeId",
                schema: "education",
                table: "TeacherSubjects",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_LevelId",
                schema: "education",
                table: "TeacherSubjects",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId1",
                schema: "education",
                table: "TeacherSubjects",
                column: "TeacherId1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentPayments_CourseEnrollmentId",
                table: "CourseEnrollmentPayments",
                column: "CourseEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentPayments_PaymentId",
                table: "CourseEnrollmentPayments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentRequests_CourseId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentRequests_RequestedByStudentId",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "RequestedByStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentRequests_Status",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_ApprovedByTeacherId",
                schema: "course",
                table: "CourseEnrollments",
                column: "ApprovedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId",
                schema: "course",
                table: "CourseEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId_StudentId",
                schema: "course",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_EnrollmentStatus",
                schema: "course",
                table: "CourseEnrollments",
                column: "EnrollmentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_StudentId",
                schema: "course",
                table: "CourseEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestGroupMembers_CourseEnrollmentRequestId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                column: "CourseEnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestGroupMembers_CourseEnrollmentRequestId_StudentId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                columns: new[] { "CourseEnrollmentRequestId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestGroupMembers_InvitedByStudentId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                column: "InvitedByStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestGroupMembers_StudentId",
                schema: "course",
                table: "CourseRequestGroupMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedAvailabilities_CourseEnrollmentRequestId",
                schema: "course",
                table: "CourseRequestSelectedAvailabilities",
                column: "CourseEnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedAvailabilities_TeacherAvailabilityId",
                schema: "course",
                table: "CourseRequestSelectedAvailabilities",
                column: "TeacherAvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CurriculumId",
                schema: "course",
                table: "Courses",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_GradeId",
                schema: "course",
                table: "Courses",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_LevelId",
                schema: "course",
                table: "Courses",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SessionTypeId",
                schema: "course",
                table: "Courses",
                column: "SessionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status",
                schema: "course",
                table: "Courses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SubjectId",
                schema: "course",
                table: "Courses",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherId",
                schema: "course",
                table: "Courses",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeachingModeId_SessionTypeId",
                schema: "course",
                table: "Courses",
                columns: new[] { "TeachingModeId", "SessionTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_CourseEnrollmentId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_Date",
                schema: "course",
                table: "CourseSchedules",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_LocationId",
                schema: "course",
                table: "CourseSchedules",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_Status",
                schema: "course",
                table: "CourseSchedules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_TeacherAvailabilityId",
                schema: "course",
                table: "CourseSchedules",
                column: "TeacherAvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_TeachingModeId",
                schema: "course",
                table: "CourseSchedules",
                column: "TeachingModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseId",
                schema: "course",
                table: "CourseSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseId_SessionNumber",
                schema: "course",
                table: "CourseSessions",
                columns: new[] { "CourseId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_Email",
                schema: "student",
                table: "Guardians",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_IsActive",
                schema: "student",
                table: "Guardians",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_Phone",
                schema: "student",
                table: "Guardians",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_UserId",
                schema: "student",
                table: "Guardians",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItems_PaymentId",
                table: "PaymentItems",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayerUserId",
                table: "Payments",
                column: "PayerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_Date",
                schema: "session",
                table: "ScheduledSessions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_LocationId",
                schema: "session",
                table: "ScheduledSessions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_SessionId",
                schema: "session",
                table: "ScheduledSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_Status",
                schema: "session",
                table: "ScheduledSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_TeachingModeId",
                schema: "session",
                table: "ScheduledSessions",
                column: "TeachingModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_TimeSlotId",
                schema: "session",
                table: "ScheduledSessions",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestOffers_SessionRequestId",
                schema: "session",
                table: "SessionRequestOffers",
                column: "SessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestOffers_Status",
                schema: "session",
                table: "SessionRequestOffers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequestOffers_TeacherId",
                schema: "session",
                table: "SessionRequestOffers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_CurriculumId",
                schema: "session",
                table: "SessionRequests",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_LevelId",
                schema: "session",
                table: "SessionRequests",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_LocationId",
                schema: "session",
                table: "SessionRequests",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_SessionTypeId",
                schema: "session",
                table: "SessionRequests",
                column: "SessionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_Status",
                schema: "session",
                table: "SessionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_StudentId",
                schema: "session",
                table: "SessionRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_SubjectId",
                schema: "session",
                table: "SessionRequests",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_TeachingModeId_SessionTypeId",
                schema: "session",
                table: "SessionRequests",
                columns: new[] { "TeachingModeId", "SessionTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionRequestId",
                schema: "session",
                table: "Sessions",
                column: "SessionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                schema: "session",
                table: "Sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StudentId",
                schema: "session",
                table: "Sessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TeacherId",
                schema: "session",
                table: "Sessions",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CurriculumId",
                schema: "student",
                table: "Students",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_DomainId_CurriculumId_LevelId_GradeId",
                schema: "student",
                table: "Students",
                columns: new[] { "DomainId", "CurriculumId", "LevelId", "GradeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_GradeId",
                schema: "student",
                table: "Students",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_GuardianId",
                schema: "student",
                table: "Students",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_IsActive",
                schema: "student",
                table: "Students",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Students_IsMinor",
                schema: "student",
                table: "Students",
                column: "IsMinor");

            migrationBuilder.CreateIndex(
                name: "IX_Students_LevelId",
                schema: "student",
                table: "Students",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                schema: "student",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAreas_LocationId",
                table: "TeacherAreas",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAreas_TeacherId",
                table: "TeacherAreas",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAuditLogs_TeacherId",
                table: "TeacherAuditLogs",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilities_DayOfWeekId",
                table: "TeacherAvailabilities",
                column: "DayOfWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilities_TeacherId",
                table: "TeacherAvailabilities",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilities_TimeSlotId",
                table: "TeacherAvailabilities",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilityExceptions_TeacherId",
                table: "TeacherAvailabilityExceptions",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAvailabilityExceptions_TimeSlotId",
                table: "TeacherAvailabilityExceptions",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_TeacherId",
                table: "TeacherDocuments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_StudentId",
                table: "TeacherReviews",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_TeacherId",
                table: "TeacherReviews",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_UserId",
                table: "Teachers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                column: "TeacherSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_TeacherSubjectId_UnitId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                columns: new[] { "TeacherSubjectId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectUnits_UnitId",
                schema: "teacher",
                table: "TeacherSubjectUnits",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Curriculums_CurriculumId",
                schema: "education",
                table: "TeacherSubjects",
                column: "CurriculumId",
                principalSchema: "education",
                principalTable: "Curriculums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_EducationLevels_LevelId",
                schema: "education",
                table: "TeacherSubjects",
                column: "LevelId",
                principalSchema: "education",
                principalTable: "EducationLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Grades_GradeId",
                schema: "education",
                table: "TeacherSubjects",
                column: "GradeId",
                principalSchema: "education",
                principalTable: "Grades",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Teachers_TeacherId",
                schema: "education",
                table: "TeacherSubjects",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Teachers_TeacherId1",
                schema: "education",
                table: "TeacherSubjects",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Curriculums_CurriculumId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_EducationLevels_LevelId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Grades_GradeId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Teachers_TeacherId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Teachers_TeacherId1",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropTable(
                name: "CourseEnrollmentPayments");

            migrationBuilder.DropTable(
                name: "CourseRequestGroupMembers",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseRequestSelectedAvailabilities",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseSchedules",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseSessions",
                schema: "course");

            migrationBuilder.DropTable(
                name: "PaymentItems");

            migrationBuilder.DropTable(
                name: "ScheduledSessions",
                schema: "session");

            migrationBuilder.DropTable(
                name: "SessionRequestOffers",
                schema: "session");

            migrationBuilder.DropTable(
                name: "TeacherAreas");

            migrationBuilder.DropTable(
                name: "TeacherAuditLogs");

            migrationBuilder.DropTable(
                name: "TeacherAvailabilityExceptions");

            migrationBuilder.DropTable(
                name: "TeacherDocuments");

            migrationBuilder.DropTable(
                name: "TeacherReviews");

            migrationBuilder.DropTable(
                name: "TeacherSubjectUnits",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "CourseEnrollmentRequests",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseEnrollments",
                schema: "course");

            migrationBuilder.DropTable(
                name: "TeacherAvailabilities");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "session");

            migrationBuilder.DropTable(
                name: "Courses",
                schema: "course");

            migrationBuilder.DropTable(
                name: "DaysOfWeek");

            migrationBuilder.DropTable(
                name: "SessionRequests",
                schema: "session");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "student");

            migrationBuilder.DropTable(
                name: "Guardians",
                schema: "student");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_CurriculumId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_GradeId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_LevelId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_TeacherId1",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "CanTeachFullSubject",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "GradeId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "LevelId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Users_TeacherId",
                schema: "education",
                table: "TeacherSubjects",
                column: "TeacherId",
                principalSchema: "security",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
