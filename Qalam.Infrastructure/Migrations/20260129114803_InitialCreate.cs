using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "education");

            migrationBuilder.EnsureSchema(
                name: "course");

            migrationBuilder.EnsureSchema(
                name: "teaching");

            migrationBuilder.EnsureSchema(
                name: "student");

            migrationBuilder.EnsureSchema(
                name: "common");

            migrationBuilder.EnsureSchema(
                name: "quran");

            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.EnsureSchema(
                name: "session");

            migrationBuilder.EnsureSchema(
                name: "teacher");

            migrationBuilder.CreateTable(
                name: "DaysOfWeek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
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
                name: "EducationDomains",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttemptTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpLoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "common",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentLocationId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Locations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalSchema: "common",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuranContentTypes",
                schema: "quran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuranContentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuranLevels",
                schema: "quran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuranLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuranParts",
                schema: "quran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartNumber = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuranParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuranSurahs",
                schema: "quran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurahNumber = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AyahCount = table.Column<int>(type: "int", nullable: false),
                    PartNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuranSurahs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionTypes",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "common",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeachingModes",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                schema: "common",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordExpiryDays = table.Column<int>(type: "int", nullable: false),
                    GoogleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FacebookId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MicrosoftId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Curriculums",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Curriculums_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EducationRules",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    HasCurriculum = table.Column<bool>(type: "bit", nullable: false),
                    HasEducationLevel = table.Column<bool>(type: "bit", nullable: false),
                    HasGrade = table.Column<bool>(type: "bit", nullable: false),
                    HasAcademicTerm = table.Column<bool>(type: "bit", nullable: false),
                    HasContentUnits = table.Column<bool>(type: "bit", nullable: false),
                    HasLessons = table.Column<bool>(type: "bit", nullable: false),
                    RequiresQuranContentType = table.Column<bool>(type: "bit", nullable: false),
                    RequiresQuranLevel = table.Column<bool>(type: "bit", nullable: false),
                    MinSessions = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MaxSessions = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    DefaultSessionDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    AllowExtension = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowFlexibleCourses = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxGroupSize = table.Column<int>(type: "int", nullable: true),
                    MinGroupSize = table.Column<int>(type: "int", nullable: true),
                    NotesAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotesEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationRules", x => x.Id);
                    table.CheckConstraint("CK_EducationRules_GroupRange", "([MinGroupSize] IS NULL AND [MaxGroupSize] IS NULL) OR ([MinGroupSize] <= [MaxGroupSize])");
                    table.CheckConstraint("CK_EducationRules_SessionsRange", "[MinSessions] <= [MaxSessions]");
                    table.ForeignKey(
                        name: "FK_EducationRules_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "security",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DomainTeachingModes",
                schema: "teaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    TeachingModeId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainTeachingModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DomainTeachingModes_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DomainTeachingModes_TeachingModes_TeachingModeId",
                        column: x => x.TeachingModeId,
                        principalSchema: "teaching",
                        principalTable: "TeachingModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailConfirmationOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfirmationOtps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConfirmationOtps_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "LoginSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOtps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetOtps_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "PhoneConfirmationOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneConfirmationOtps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneConfirmationOtps_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WasNotified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityEvents_Users_UserId",
                        column: x => x.UserId,
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
                    Location = table.Column<int>(type: "int", nullable: true),
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
                name: "TrustedDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrustedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustedDevices_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TwoFactorRecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFactorRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TwoFactorRecoveryCodes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "security",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JwtId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    AddedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "security",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "security",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "security",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurriculumId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicTerms_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EducationLevels",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationLevels_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EducationLevels_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
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
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    ReviewedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdentityType = table.Column<int>(type: "int", nullable: true),
                    IssuingCountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    CertificateTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Issuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                name: "Grades",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grades_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "Subjects",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    LevelId = table.Column<int>(type: "int", nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    TermId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_AcademicTerms_TermId",
                        column: x => x.TermId,
                        principalSchema: "education",
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_EducationDomains_DomainId",
                        column: x => x.DomainId,
                        principalSchema: "education",
                        principalTable: "EducationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "education",
                        principalTable: "Grades",
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
                name: "ContentUnits",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    UnitTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuranSurahId = table.Column<int>(type: "int", nullable: true),
                    QuranPartId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentUnits", x => x.Id);
                    table.CheckConstraint("CK_ContentUnits_QuranPartLink", "([UnitTypeCode] <> 'QuranPart') OR ([QuranPartId] IS NOT NULL)");
                    table.CheckConstraint("CK_ContentUnits_QuranSurahLink", "([UnitTypeCode] <> 'QuranSurah') OR ([QuranSurahId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ContentUnits_QuranParts_QuranPartId",
                        column: x => x.QuranPartId,
                        principalSchema: "quran",
                        principalTable: "QuranParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentUnits_QuranSurahs_QuranSurahId",
                        column: x => x.QuranSurahId,
                        principalSchema: "quran",
                        principalTable: "QuranSurahs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentUnits_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "education",
                        principalTable: "Subjects",
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
                name: "TeacherSubjects",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: true),
                    LevelId = table.Column<int>(type: "int", nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: true),
                    CanTeachFullSubject = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TeacherId1 = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalSchema: "education",
                        principalTable: "Curriculums",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_EducationLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "education",
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "education",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Teachers_TeacherId1",
                        column: x => x.TeacherId1,
                        principalTable: "Teachers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_ContentUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
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
                name: "IX_AcademicTerms_CurriculumId",
                schema: "education",
                table: "AcademicTerms",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_CurriculumId_NameEn",
                schema: "education",
                table: "AcademicTerms",
                columns: new[] { "CurriculumId", "NameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentUnits_IsActive",
                schema: "education",
                table: "ContentUnits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContentUnits_QuranPartId",
                schema: "education",
                table: "ContentUnits",
                column: "QuranPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentUnits_QuranSurahId",
                schema: "education",
                table: "ContentUnits",
                column: "QuranSurahId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentUnits_SubjectId_UnitTypeCode_NameEn",
                schema: "education",
                table: "ContentUnits",
                columns: new[] { "SubjectId", "UnitTypeCode", "NameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentUnits_SubjectId_UnitTypeCode_OrderIndex",
                schema: "education",
                table: "ContentUnits",
                columns: new[] { "SubjectId", "UnitTypeCode", "OrderIndex" });

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
                name: "IX_Curriculums_DomainId",
                schema: "education",
                table: "Curriculums",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_DomainId_NameEn",
                schema: "education",
                table: "Curriculums",
                columns: new[] { "DomainId", "NameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DaysOfWeek_IsActive",
                table: "DaysOfWeek",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DaysOfWeek_OrderIndex",
                table: "DaysOfWeek",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_DomainTeachingModes_DomainId_TeachingModeId",
                schema: "teaching",
                table: "DomainTeachingModes",
                columns: new[] { "DomainId", "TeachingModeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainTeachingModes_TeachingModeId",
                schema: "teaching",
                table: "DomainTeachingModes",
                column: "TeachingModeId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationDomains_Code",
                schema: "education",
                table: "EducationDomains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationDomains_IsActive",
                schema: "education",
                table: "EducationDomains",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevels_CurriculumId",
                schema: "education",
                table: "EducationLevels",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevels_DomainId",
                schema: "education",
                table: "EducationLevels",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevels_DomainId_CurriculumId_NameEn",
                schema: "education",
                table: "EducationLevels",
                columns: new[] { "DomainId", "CurriculumId", "NameEn" },
                unique: true,
                filter: "[CurriculumId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EducationRules_DomainId",
                schema: "teaching",
                table: "EducationRules",
                column: "DomainId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationOtps_UserId",
                table: "EmailConfirmationOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_LevelId",
                schema: "education",
                table: "Grades",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_LevelId_NameEn",
                schema: "education",
                table: "Grades",
                columns: new[] { "LevelId", "NameEn" },
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
                name: "IX_Lessons_UnitId",
                schema: "education",
                table: "Lessons",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_NameEn",
                schema: "education",
                table: "Lessons",
                columns: new[] { "UnitId", "NameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_IsActive",
                schema: "common",
                table: "Locations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ParentLocationId",
                schema: "common",
                table: "Locations",
                column: "ParentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Type",
                schema: "common",
                table: "Locations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_LoginSessions_UserId",
                table: "LoginSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_UserId",
                table: "PasswordResetOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItems_PaymentId",
                table: "PaymentItems",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayerUserId",
                table: "Payments",
                column: "PayerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneConfirmationOtps_UserId",
                table: "PhoneConfirmationOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuranContentTypes_Code",
                schema: "quran",
                table: "QuranContentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuranContentTypes_IsActive",
                schema: "quran",
                table: "QuranContentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuranLevels_IsActive",
                schema: "quran",
                table: "QuranLevels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuranLevels_OrderIndex",
                schema: "quran",
                table: "QuranLevels",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_QuranParts_PartNumber",
                schema: "quran",
                table: "QuranParts",
                column: "PartNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuranSurahs_SurahNumber",
                schema: "quran",
                table: "QuranSurahs",
                column: "SurahNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "security",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "security",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

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
                name: "IX_SecurityEvents_UserId",
                table: "SecurityEvents",
                column: "UserId");

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
                name: "IX_SessionTypes_Code",
                schema: "teaching",
                table: "SessionTypes",
                column: "Code",
                unique: true);

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
                name: "IX_Subjects_CurriculumId_LevelId_GradeId_TermId",
                schema: "education",
                table: "Subjects",
                columns: new[] { "CurriculumId", "LevelId", "GradeId", "TermId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DomainId",
                schema: "education",
                table: "Subjects",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DomainId_Code",
                schema: "education",
                table: "Subjects",
                columns: new[] { "DomainId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_GradeId",
                schema: "education",
                table: "Subjects",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_IsActive",
                schema: "education",
                table: "Subjects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_LevelId",
                schema: "education",
                table: "Subjects",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TermId",
                schema: "education",
                table: "Subjects",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_IsPublic",
                schema: "common",
                table: "SystemSettings",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                schema: "common",
                table: "SystemSettings",
                column: "Key",
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
                name: "IX_TeacherSubjects_SubjectId",
                schema: "education",
                table: "TeacherSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId_SubjectId",
                schema: "education",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId1",
                schema: "education",
                table: "TeacherSubjects",
                column: "TeacherId1");

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

            migrationBuilder.CreateIndex(
                name: "IX_TeachingModes_Code",
                schema: "teaching",
                table: "TeachingModes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_IsActive",
                schema: "common",
                table: "TimeSlots",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_StartTime_EndTime",
                schema: "common",
                table: "TimeSlots",
                columns: new[] { "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_UserId",
                table: "TrustedDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorRecoveryCodes_UserId",
                table: "TwoFactorRecoveryCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "security",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "security",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "security",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "security",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "security",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

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
                name: "DomainTeachingModes",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "EducationRules",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "EmailConfirmationOtps");

            migrationBuilder.DropTable(
                name: "IpLoginAttempts");

            migrationBuilder.DropTable(
                name: "Lessons",
                schema: "education");

            migrationBuilder.DropTable(
                name: "LoginSessions");

            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "PasswordResetOtps");

            migrationBuilder.DropTable(
                name: "PaymentItems");

            migrationBuilder.DropTable(
                name: "PhoneConfirmationOtps");

            migrationBuilder.DropTable(
                name: "QuranContentTypes",
                schema: "quran");

            migrationBuilder.DropTable(
                name: "QuranLevels",
                schema: "quran");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "ScheduledSessions",
                schema: "session");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "SessionRequestOffers",
                schema: "session");

            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "common");

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
                name: "TrustedDevices");

            migrationBuilder.DropTable(
                name: "TwoFactorRecoveryCodes");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "security");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "security");

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
                name: "ContentUnits",
                schema: "education");

            migrationBuilder.DropTable(
                name: "TeacherSubjects",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "Courses",
                schema: "course");

            migrationBuilder.DropTable(
                name: "DaysOfWeek");

            migrationBuilder.DropTable(
                name: "TimeSlots",
                schema: "common");

            migrationBuilder.DropTable(
                name: "SessionRequests",
                schema: "session");

            migrationBuilder.DropTable(
                name: "QuranParts",
                schema: "quran");

            migrationBuilder.DropTable(
                name: "QuranSurahs",
                schema: "quran");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "common");

            migrationBuilder.DropTable(
                name: "SessionTypes",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "student");

            migrationBuilder.DropTable(
                name: "Subjects",
                schema: "education");

            migrationBuilder.DropTable(
                name: "TeachingModes",
                schema: "teaching");

            migrationBuilder.DropTable(
                name: "Guardians",
                schema: "student");

            migrationBuilder.DropTable(
                name: "AcademicTerms",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Grades",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "security");

            migrationBuilder.DropTable(
                name: "EducationLevels",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Curriculums",
                schema: "education");

            migrationBuilder.DropTable(
                name: "EducationDomains",
                schema: "education");
        }
    }
}
