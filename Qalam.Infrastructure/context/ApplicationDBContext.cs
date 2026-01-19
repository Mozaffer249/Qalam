using EntityFrameworkCore.EncryptColumn.Extension;
using EntityFrameworkCore.EncryptColumn.Interfaces;
using EntityFrameworkCore.EncryptColumn.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Quran;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Student;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Session;
using Qalam.Data.Entity.Payment;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Qalam.Infrastructure.context
{
    public class ApplicationDBContext : IdentityDbContext<User, Role, int, IdentityUserClaim<int>,
          IdentityUserRole<int>, IdentityUserLogin<int>, IdentityRoleClaim<int>,
          IdentityUserToken<int>>
    {
        private readonly IEncryptionProvider _encryptionProvider;

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
            _encryptionProvider = new GenerateEncryptionProvider("8a4dcaaec64d412380fe4b02193cd26f");
        }

        // Identity DbSets
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<LoginSession> LoginSessions { get; set; }
        public DbSet<TrustedDevice> TrustedDevices { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SecurityEvent> SecurityEvents { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes { get; set; }
        public DbSet<EmailConfirmationOtp> EmailConfirmationOtps { get; set; }
        public DbSet<PhoneConfirmationOtp> PhoneConfirmationOtps { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<IpLoginAttempt> IpLoginAttempts { get; set; }

        // Education Schema DbSets
        public DbSet<EducationDomain> EducationDomains { get; set; }
        public DbSet<Curriculum> Curriculums { get; set; }
        public DbSet<EducationLevel> EducationLevels { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<AcademicTerm> AcademicTerms { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ContentUnit> ContentUnits { get; set; }
        public DbSet<Lesson> Lessons { get; set; }

        // Quran Schema DbSets
        public DbSet<QuranLevel> QuranLevels { get; set; }
        public DbSet<QuranContentType> QuranContentTypes { get; set; }
        public DbSet<QuranPart> QuranParts { get; set; }
        public DbSet<QuranSurah> QuranSurahs { get; set; }

        // Teaching Schema DbSets
        public DbSet<TeachingMode> TeachingModes { get; set; }
        public DbSet<SessionType> SessionTypes { get; set; }
        public DbSet<DomainTeachingMode> DomainTeachingModes { get; set; }
        public DbSet<EducationRule> EducationRules { get; set; }

        // Common Schema DbSets
        public DbSet<Location> Locations { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<DayOfWeekMaster> DaysOfWeek { get; set; }

        // Student Schema DbSets
        public DbSet<Student> Students { get; set; }
        public DbSet<Guardian> Guardians { get; set; }

        // Teacher Schema DbSets
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherDocument> TeacherDocuments { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<TeacherSubjectUnit> TeacherSubjectUnits { get; set; }
        public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
        public DbSet<TeacherAvailabilityException> TeacherAvailabilityExceptions { get; set; }
        public DbSet<TeacherArea> TeacherAreas { get; set; }
        public DbSet<TeacherReview> TeacherReviews { get; set; }
        public DbSet<TeacherAuditLog> TeacherAuditLogs { get; set; }

        // Course Schema DbSets
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSession> CourseSessions { get; set; }
        public DbSet<CourseEnrollmentRequest> CourseEnrollmentRequests { get; set; }
        public DbSet<CourseRequestSelectedAvailability> CourseRequestSelectedAvailabilities { get; set; }
        public DbSet<CourseRequestGroupMember> CourseRequestGroupMembers { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<CourseSchedule> CourseSchedules { get; set; }

        // Session Schema DbSets
        public DbSet<SessionRequest> SessionRequests { get; set; }
        public DbSet<SessionRequestOffer> SessionRequestOffers { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<ScheduledSession> ScheduledSessions { get; set; }

        // Payment Schema DbSets
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentItem> PaymentItems { get; set; }
        public DbSet<CourseEnrollmentPayment> CourseEnrollmentPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all entity configurations from the Infrastructure project
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Identity table mappings
            builder.Entity<User>().ToTable("Users", "security");
            builder.Entity<Role>().ToTable("Roles", "security");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles", "security");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims", "security");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins", "security");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims", "security");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens", "security");

            // Apply encryption
            builder.UseEncryption(_encryptionProvider);

            // Apply global query filters for soft delete
            ApplySoftDeleteQueryFilters(builder);
        }

        private void ApplySoftDeleteQueryFilters(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(SoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(SoftDeletableEntity.IsDeleted));
                    var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);

                    entityType.SetQueryFilter(filter);
                }
            }
        }
    }
}
