// -----------------------------------------------------------------------------
// EF Core Entities + Fluent API Configurations (UpperCamelCase)
// Covers all analyzed tables: Admin Master (TimeSlots, DaysOfWeek, Locations),
// Teacher, Availability, Courses, Requests, Scheduling, Reviews, Audit Logs,
// Payments, Payment Items, Enrollment Payments.
// -----------------------------------------------------------------------------
//
// Notes:
// 1) External referenced entities (Students, Subjects, Curriculums, Stages, Levels, Units, Users)
//    are represented here as FK int properties only (no navigation) to keep this drop-in.
//    If you already have these entities, you can add navigations easily.
// 2) TimeSlot class is assumed to exist in your project; included here as a reference model.
// 3) Enums are stored as ints by default (EF Core default). If you prefer strings, add conversions.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Domain.Entities
{
    // -------------------------
    // Base
    // -------------------------
    public abstract class AuditableEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    // -------------------------
    // Enums
    // -------------------------
    public enum TeacherStatus
    {
        Pending = 1,
        Active = 2,
        Blocked = 3
    }

    public enum TeacherDocumentType
    {
        Id = 1,
        Certificate = 2,
        Other = 3
    }

    public enum TeachingMode
    {
        InPerson = 1,
        Online = 2
    }

    public enum SessionType
    {
        Individual = 1,
        Group = 2
    }

    public enum CourseStatus
    {
        Draft = 1,
        Published = 2,
        Paused = 3
    }

    public enum RequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4
    }

    public enum GroupMemberConfirmationStatus
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3
    }

    public enum EnrollmentStatus
    {
        PendingPayment = 1,
        Active = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum ScheduleStatus
    {
        Scheduled = 1,
        Completed = 2,
        Cancelled = 3,
        Rescheduled = 4
    }

    public enum AvailabilityExceptionType
    {
        Blocked = 1,
        Extra = 2
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5
    }

    public enum PaymentItemType
    {
        CourseEnrollment = 1,
        SessionBooking = 2,
        PackageSubscription = 3
    }

    // -------------------------
    // Admin Master Data
    // -------------------------

    // Your existing TimeSlot (included here for completeness; if already exists, keep one copy only)
    public class TimeSlot : AuditableEntity
    {
        public int Id { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DurationMinutes { get; set; }

        [MaxLength(50)]
        public string? LabelAr { get; set; }

        [MaxLength(50)]
        public string? LabelEn { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigations
        public ICollection<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
        public ICollection<TeacherAvailabilityException> TeacherAvailabilityExceptions { get; set; } = new List<TeacherAvailabilityException>();
    }

    public class DayOfWeekMaster : AuditableEntity
    {
        public int DayOfWeekId { get; set; } // 1..7
        [MaxLength(30)]
        public string NameAr { get; set; } = null!;
        [MaxLength(30)]
        public string NameEn { get; set; } = null!;
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
    }

    public class Location : AuditableEntity
    {
        public int LocationId { get; set; }

        [MaxLength(120)]
        public string NameAr { get; set; } = null!;

        [MaxLength(120)]
        public string NameEn { get; set; } = null!;

        [MaxLength(120)]
        public string City { get; set; } = null!;

        [MaxLength(120)]
        public string Region { get; set; } = null!;

        [MaxLength(250)]
        public string Address { get; set; } = null!;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<TeacherArea> TeacherAreas { get; set; } = new List<TeacherArea>();
        public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
        public ICollection<SessionRequest> SessionRequests { get; set; } = new List<SessionRequest>();
    }

    // -------------------------
    // Teacher Core
    // -------------------------
    public class Teacher : AuditableEntity
    {
        public int TeacherId { get; set; }

        // If you have Users table:
        public int? UserId { get; set; }

        [MaxLength(200)]
        public string? Bio { get; set; }

        public TeacherStatus Status { get; set; } = TeacherStatus.Pending;

        public decimal RatingAverage { get; set; } = 0m;

        public ICollection<TeacherDocument> TeacherDocuments { get; set; } = new List<TeacherDocument>();
        public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
        public ICollection<TeacherAvailability> TeacherAvailabilities { get; set; } = new List<TeacherAvailability>();
        public ICollection<TeacherAvailabilityException> TeacherAvailabilityExceptions { get; set; } = new List<TeacherAvailabilityException>();
        public ICollection<TeacherArea> TeacherAreas { get; set; } = new List<TeacherArea>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
        public ICollection<SessionRequestOffer> SessionRequestOffers { get; set; } = new List<SessionRequestOffer>();
        public ICollection<TeacherReview> TeacherReviews { get; set; } = new List<TeacherReview>();
        public ICollection<TeacherAuditLog> TeacherAuditLogs { get; set; } = new List<TeacherAuditLog>();
    }

    public class TeacherDocument : AuditableEntity
    {
        public int TeacherDocumentId { get; set; }
        public int TeacherId { get; set; }
        public TeacherDocumentType DocumentType { get; set; }

        [MaxLength(400)]
        public string FilePath { get; set; } = null!;

        public bool IsVerified { get; set; } = false;

        // Admin user id (if you have Admin/Users):
        public int? VerifiedByAdminId { get; set; }

        public Teacher Teacher { get; set; } = null!;
    }

    public class TeacherSubject : AuditableEntity
    {
        public int TeacherSubjectId { get; set; }
        public int TeacherId { get; set; }

        // External refs (admin content tables)
        public int SubjectId { get; set; }
        public int CurriculumId { get; set; }
        public int StageId { get; set; }
        public int LevelId { get; set; }

        public bool CanTeachFullSubject { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public Teacher Teacher { get; set; } = null!;
        public ICollection<TeacherSubjectUnit> TeacherSubjectUnits { get; set; } = new List<TeacherSubjectUnit>();
    }

    public class TeacherSubjectUnit : AuditableEntity
    {
        public int TeacherSubjectUnitId { get; set; }
        public int TeacherSubjectId { get; set; }
        public int UnitId { get; set; } // External: Content_Units

        public TeacherSubject TeacherSubject { get; set; } = null!;
    }

    public class TeacherAvailability : AuditableEntity
    {
        public int TeacherAvailabilityId { get; set; }
        public int TeacherId { get; set; }

        public int DayOfWeekId { get; set; } // FK -> DaysOfWeek
        public int TimeSlotId { get; set; }  // FK -> TimeSlots

        public bool IsActive { get; set; } = true;

        public Teacher Teacher { get; set; } = null!;
        public DayOfWeekMaster DayOfWeek { get; set; } = null!;
        public TimeSlot TimeSlot { get; set; } = null!;

        public ICollection<CourseRequestSelectedAvailability> CourseRequestSelectedAvailabilities { get; set; } = new List<CourseRequestSelectedAvailability>();
        public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
    }

    public class TeacherAvailabilityException : AuditableEntity
    {
        public int TeacherAvailabilityExceptionId { get; set; }
        public int TeacherId { get; set; }

        public DateOnly Date { get; set; }
        public int TimeSlotId { get; set; }

        public AvailabilityExceptionType ExceptionType { get; set; }
        [MaxLength(250)]
        public string? Reason { get; set; }

        public Teacher Teacher { get; set; } = null!;
        public TimeSlot TimeSlot { get; set; } = null!;
    }

    public class TeacherArea : AuditableEntity
    {
        public int TeacherAreaId { get; set; }
        public int TeacherId { get; set; }
        public int LocationId { get; set; }

        public decimal MaxDistanceKm { get; set; } = 0m;
        public bool IsActive { get; set; } = true;

        public Teacher Teacher { get; set; } = null!;
        public Location Location { get; set; } = null!;
    }

    // -------------------------
    // Courses + Enrollment Requests + Scheduling
    // -------------------------
    public class Course : AuditableEntity
    {
        public int CourseId { get; set; }
        public int TeacherId { get; set; }

        // External content refs
        public int SubjectId { get; set; }
        public int CurriculumId { get; set; }
        public int StageId { get; set; }
        public int LevelId { get; set; }

        public TeachingMode TeachingMode { get; set; }
        public SessionType SessionType { get; set; }
        public bool IsFlexible { get; set; }
        public int? SessionsCount { get; set; }

        public int? SessionDurationMinutes { get; set; }

        public decimal Price { get; set; }
        public int? MaxStudents { get; set; }

        public bool CanIncludeInPackages { get; set; } = false;
        public CourseStatus Status { get; set; } = CourseStatus.Draft;

        public Teacher Teacher { get; set; } = null!;
        public ICollection<CourseSession> CourseSessions { get; set; } = new List<CourseSession>();
        public ICollection<CourseEnrollmentRequest> CourseEnrollmentRequests { get; set; } = new List<CourseEnrollmentRequest>();
        public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
    }

    public class CourseSession : AuditableEntity
    {
        public int CourseSessionId { get; set; }
        public int CourseId { get; set; }

        public int SessionNumber { get; set; }

        [MaxLength(150)]
        public string Title { get; set; } = null!;

        public int DurationMinutes { get; set; }

        public Course Course { get; set; } = null!;
    }

    public class CourseEnrollmentRequest : AuditableEntity
    {
        public int CourseEnrollmentRequestId { get; set; }
        public int CourseId { get; set; }

        public int RequestedByStudentId { get; set; } // External: Students

        public TeachingMode TeachingMode { get; set; } // should be compatible with Course.TeachingMode
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [MaxLength(400)]
        public string? Notes { get; set; }

        public Course Course { get; set; } = null!;
        public ICollection<CourseRequestSelectedAvailability> SelectedAvailabilities { get; set; } = new List<CourseRequestSelectedAvailability>();
        public ICollection<CourseRequestGroupMember> GroupMembers { get; set; } = new List<CourseRequestGroupMember>();
    }

    public class CourseRequestSelectedAvailability : AuditableEntity
    {
        public int CourseRequestSelectedAvailabilityId { get; set; }
        public int CourseEnrollmentRequestId { get; set; }
        public int TeacherAvailabilityId { get; set; }
        public int? PriorityOrder { get; set; }

        public CourseEnrollmentRequest CourseEnrollmentRequest { get; set; } = null!;
        public TeacherAvailability TeacherAvailability { get; set; } = null!;
    }

    public class CourseRequestGroupMember : AuditableEntity
    {
        public int CourseRequestGroupMemberId { get; set; }
        public int CourseEnrollmentRequestId { get; set; }

        public int StudentId { get; set; } // External: Students
        public int InvitedByStudentId { get; set; } // External: Students

        public GroupMemberConfirmationStatus ConfirmationStatus { get; set; } = GroupMemberConfirmationStatus.Pending;
        public DateTime? ConfirmedAt { get; set; }

        public CourseEnrollmentRequest CourseEnrollmentRequest { get; set; } = null!;
    }

    public class CourseEnrollment : AuditableEntity
    {
        public int CourseEnrollmentId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; } // External: Students

        public int ApprovedByTeacherId { get; set; }
        public DateTime ApprovedAt { get; set; }

        public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.PendingPayment;

        public Course Course { get; set; } = null!;
        public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
        public ICollection<CourseEnrollmentPayment> CourseEnrollmentPayments { get; set; } = new List<CourseEnrollmentPayment>();
    }

    public class CourseSchedule : AuditableEntity
    {
        public int CourseScheduleId { get; set; }
        public int CourseEnrollmentId { get; set; }

        public DateOnly Date { get; set; }
        public int TeacherAvailabilityId { get; set; }

        public TeachingMode TeachingMode { get; set; }

        public int? LocationId { get; set; } // Required if InPerson

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        public CourseEnrollment CourseEnrollment { get; set; } = null!;
        public TeacherAvailability TeacherAvailability { get; set; } = null!;
        public Location? Location { get; set; }
    }

    // -------------------------
    // Session Requests (student creates, teachers offer)
    // -------------------------
    public class SessionRequest : AuditableEntity
    {
        public int SessionRequestId { get; set; }
        public int StudentId { get; set; } // External: Students

        // External content refs
        public int SubjectId { get; set; }
        public int CurriculumId { get; set; }
        public int LevelId { get; set; }

        public TeachingMode TeachingMode { get; set; }
        public SessionType SessionType { get; set; } // Individual/Group

        public int? LocationId { get; set; } // InPerson -> selected from Locations

        [MaxLength(800)]
        public string? Description { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public Location? Location { get; set; }
        public ICollection<SessionRequestOffer> Offers { get; set; } = new List<SessionRequestOffer>();
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }

    public class SessionRequestOffer : AuditableEntity
    {
        public int SessionRequestOfferId { get; set; }
        public int SessionRequestId { get; set; }
        public int TeacherId { get; set; }

        public decimal ProposedPrice { get; set; }

        [MaxLength(800)]
        public string? ProposedSchedule { get; set; } // Keep as text now; can normalize later.

        [MaxLength(500)]
        public string? Notes { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public SessionRequest SessionRequest { get; set; } = null!;
        public Teacher Teacher { get; set; } = null!;
    }

    public class Session : AuditableEntity
    {
        public int SessionId { get; set; }
        public int SessionRequestId { get; set; }

        public int TeacherId { get; set; }
        public int StudentId { get; set; } // External: Students (owner/primary)

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        public SessionRequest SessionRequest { get; set; } = null!;
        public ICollection<ScheduledSession> ScheduledSessions { get; set; } = new List<ScheduledSession>();
    }

    public class ScheduledSession : AuditableEntity
    {
        public int ScheduledSessionId { get; set; }
        public int SessionId { get; set; }

        public DateOnly Date { get; set; }
        public int TimeSlotId { get; set; } // Using TimeSlot for session scheduling

        public TeachingMode TeachingMode { get; set; }
        public int? LocationId { get; set; }

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        public Session Session { get; set; } = null!;
        public TimeSlot TimeSlot { get; set; } = null!;
        public Location? Location { get; set; }
    }

    // -------------------------
    // Reviews + Audit Logs
    // -------------------------
    public class TeacherReview : AuditableEntity
    {
        public int TeacherReviewId { get; set; }
        public int TeacherId { get; set; }
        public int StudentId { get; set; } // External: Students
        public int? SessionId { get; set; } // optional link

        public int Rating { get; set; } // 1..5
        [MaxLength(600)]
        public string? Feedback { get; set; }

        public Teacher Teacher { get; set; } = null!;
    }

    public class TeacherAuditLog : AuditableEntity
    {
        public int TeacherAuditLogId { get; set; }
        public int TeacherId { get; set; }

        [MaxLength(80)]
        public string Action { get; set; } = null!;

        [MaxLength(80)]
        public string TableName { get; set; } = null!;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public Teacher Teacher { get; set; } = null!;
    }

    // -------------------------
    // Payments
    // -------------------------
    public class Payment : AuditableEntity
    {
        public int PaymentId { get; set; }

        public int PayerUserId { get; set; } // External: Users/Students

        [MaxLength(3)]
        public string Currency { get; set; } = "SAR";

        [MaxLength(40)]
        public string PaymentProvider { get; set; } = null!;

        [MaxLength(120)]
        public string? ProviderTransactionId { get; set; }

        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        [MaxLength(50)]
        public string? InvoiceNumber { get; set; }

        [MaxLength(600)]
        public string? ReceiptUrl { get; set; }

        [MaxLength(600)]
        public string? ReceiptPath { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();
        public ICollection<CourseEnrollmentPayment> CourseEnrollmentPayments { get; set; } = new List<CourseEnrollmentPayment>();
    }

    public class PaymentItem : AuditableEntity
    {
        public int PaymentItemId { get; set; }
        public int PaymentId { get; set; }

        public PaymentItemType ItemType { get; set; }
        public int ReferenceId { get; set; } // e.g., CourseEnrollmentId

        [MaxLength(200)]
        public string? Description { get; set; }

        public decimal Amount { get; set; }

        public Payment Payment { get; set; } = null!;
    }

    public class CourseEnrollmentPayment : AuditableEntity
    {
        public int CourseEnrollmentPaymentId { get; set; }
        public int CourseEnrollmentId { get; set; }
        public int PaymentId { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public CourseEnrollment CourseEnrollment { get; set; } = null!;
        public Payment Payment { get; set; } = null!;
    }
}

// -----------------------------------------------------------------------------
// DbContext + Fluent API
// -----------------------------------------------------------------------------
namespace App.Infrastructure.Persistence
{
    using App.Domain.Entities;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Admin master
        public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
        public DbSet<DayOfWeekMaster> DaysOfWeek => Set<DayOfWeekMaster>();
        public DbSet<Location> Locations => Set<Location>();

        // Teacher
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<TeacherDocument> TeacherDocuments => Set<TeacherDocument>();
        public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
        public DbSet<TeacherSubjectUnit> TeacherSubjectUnits => Set<TeacherSubjectUnit>();
        public DbSet<TeacherAvailability> TeacherAvailability => Set<TeacherAvailability>();
        public DbSet<TeacherAvailabilityException> TeacherAvailabilityExceptions => Set<TeacherAvailabilityException>();
        public DbSet<TeacherArea> TeacherAreas => Set<TeacherArea>();

        // Courses
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<CourseSession> CourseSessions => Set<CourseSession>();
        public DbSet<CourseEnrollmentRequest> CourseEnrollmentRequests => Set<CourseEnrollmentRequest>();
        public DbSet<CourseRequestSelectedAvailability> CourseRequestSelectedAvailabilities => Set<CourseRequestSelectedAvailability>();
        public DbSet<CourseRequestGroupMember> CourseRequestGroupMembers => Set<CourseRequestGroupMember>();
        public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
        public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();

        // Session requests
        public DbSet<SessionRequest> SessionRequests => Set<SessionRequest>();
        public DbSet<SessionRequestOffer> SessionRequestOffers => Set<SessionRequestOffer>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<ScheduledSession> ScheduledSessions => Set<ScheduledSession>();

        // Reviews + audit
        public DbSet<TeacherReview> TeacherReviews => Set<TeacherReview>();
        public DbSet<TeacherAuditLog> TeacherAuditLogs => Set<TeacherAuditLog>();

        // Payments
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentItem> PaymentItems => Set<PaymentItem>();
        public DbSet<CourseEnrollmentPayment> CourseEnrollmentPayments => Set<CourseEnrollmentPayment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply configurations
            modelBuilder.ApplyConfiguration(new TimeSlotConfig());
            modelBuilder.ApplyConfiguration(new DayOfWeekMasterConfig());
            modelBuilder.ApplyConfiguration(new LocationConfig());

            modelBuilder.ApplyConfiguration(new TeacherConfig());
            modelBuilder.ApplyConfiguration(new TeacherDocumentConfig());
            modelBuilder.ApplyConfiguration(new TeacherSubjectConfig());
            modelBuilder.ApplyConfiguration(new TeacherSubjectUnitConfig());
            modelBuilder.ApplyConfiguration(new TeacherAvailabilityConfig());
            modelBuilder.ApplyConfiguration(new TeacherAvailabilityExceptionConfig());
            modelBuilder.ApplyConfiguration(new TeacherAreaConfig());

            modelBuilder.ApplyConfiguration(new CourseConfig());
            modelBuilder.ApplyConfiguration(new CourseSessionConfig());
            modelBuilder.ApplyConfiguration(new CourseEnrollmentRequestConfig());
            modelBuilder.ApplyConfiguration(new CourseRequestSelectedAvailabilityConfig());
            modelBuilder.ApplyConfiguration(new CourseRequestGroupMemberConfig());
            modelBuilder.ApplyConfiguration(new CourseEnrollmentConfig());
            modelBuilder.ApplyConfiguration(new CourseScheduleConfig());

            modelBuilder.ApplyConfiguration(new SessionRequestConfig());
            modelBuilder.ApplyConfiguration(new SessionRequestOfferConfig());
            modelBuilder.ApplyConfiguration(new SessionConfig());
            modelBuilder.ApplyConfiguration(new ScheduledSessionConfig());

            modelBuilder.ApplyConfiguration(new TeacherReviewConfig());
            modelBuilder.ApplyConfiguration(new TeacherAuditLogConfig());

            modelBuilder.ApplyConfiguration(new PaymentConfig());
            modelBuilder.ApplyConfiguration(new PaymentItemConfig());
            modelBuilder.ApplyConfiguration(new CourseEnrollmentPaymentConfig());

            base.OnModelCreating(modelBuilder);
        }
    }

    // -------------------------
    // Configurations
    // -------------------------
    internal sealed class TimeSlotConfig : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> b)
        {
            b.ToTable("TimeSlots");
            b.HasKey(x => x.Id);

            b.Property(x => x.LabelAr).HasMaxLength(50);
            b.Property(x => x.LabelEn).HasMaxLength(50);

            b.HasIndex(x => new { x.StartTime, x.EndTime }).IsUnique();

            b.Property(x => x.IsActive).HasDefaultValue(true);
        }
    }

    internal sealed class DayOfWeekMasterConfig : IEntityTypeConfiguration<DayOfWeekMaster>
    {
        public void Configure(EntityTypeBuilder<DayOfWeekMaster> b)
        {
            b.ToTable("DaysOfWeek");
            b.HasKey(x => x.DayOfWeekId);

            b.Property(x => x.NameAr).HasMaxLength(30).IsRequired();
            b.Property(x => x.NameEn).HasMaxLength(30).IsRequired();

            b.HasIndex(x => x.OrderIndex);
            b.Property(x => x.IsActive).HasDefaultValue(true);
        }
    }

    internal sealed class LocationConfig : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> b)
        {
            b.ToTable("Locations");
            b.HasKey(x => x.LocationId);

            b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
            b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
            b.Property(x => x.City).HasMaxLength(120).IsRequired();
            b.Property(x => x.Region).HasMaxLength(120).IsRequired();
            b.Property(x => x.Address).HasMaxLength(250).IsRequired();

            b.HasIndex(x => new { x.City, x.Region });
            b.Property(x => x.IsActive).HasDefaultValue(true);
        }
    }

    internal sealed class TeacherConfig : IEntityTypeConfiguration<Teacher>
    {
        public void Configure(EntityTypeBuilder<Teacher> b)
        {
            b.ToTable("Teachers");
            b.HasKey(x => x.TeacherId);

            b.Property(x => x.Bio).HasMaxLength(200);
            b.Property(x => x.RatingAverage).HasPrecision(3, 2).HasDefaultValue(0m);

            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.UserId);
        }
    }

    internal sealed class TeacherDocumentConfig : IEntityTypeConfiguration<TeacherDocument>
    {
        public void Configure(EntityTypeBuilder<TeacherDocument> b)
        {
            b.ToTable("TeacherDocuments");
            b.HasKey(x => x.TeacherDocumentId);

            b.Property(x => x.FilePath).HasMaxLength(400).IsRequired();

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherDocuments)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.TeacherId);
        }
    }

    internal sealed class TeacherSubjectConfig : IEntityTypeConfiguration<TeacherSubject>
    {
        public void Configure(EntityTypeBuilder<TeacherSubject> b)
        {
            b.ToTable("TeacherSubjects");
            b.HasKey(x => x.TeacherSubjectId);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherSubjects)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.TeacherId);

            // Unique: (TeacherId, SubjectId, CurriculumId, StageId, LevelId)
            b.HasIndex(x => new { x.TeacherId, x.SubjectId, x.CurriculumId, x.StageId, x.LevelId })
                .IsUnique();

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.CanTeachFullSubject).HasDefaultValue(true);
        }
    }

    internal sealed class TeacherSubjectUnitConfig : IEntityTypeConfiguration<TeacherSubjectUnit>
    {
        public void Configure(EntityTypeBuilder<TeacherSubjectUnit> b)
        {
            b.ToTable("TeacherSubjectUnits");
            b.HasKey(x => x.TeacherSubjectUnitId);

            b.HasOne(x => x.TeacherSubject)
                .WithMany(ts => ts.TeacherSubjectUnits)
                .HasForeignKey(x => x.TeacherSubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TeacherSubjectId, x.UnitId }).IsUnique();
        }
    }

    internal sealed class TeacherAvailabilityConfig : IEntityTypeConfiguration<TeacherAvailability>
    {
        public void Configure(EntityTypeBuilder<TeacherAvailability> b)
        {
            b.ToTable("TeacherAvailability");
            b.HasKey(x => x.TeacherAvailabilityId);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherAvailabilities)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.DayOfWeek)
                .WithMany(d => d.TeacherAvailabilities)
                .HasForeignKey(x => x.DayOfWeekId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.TimeSlot)
                .WithMany(ts => ts.TeacherAvailabilities)
                .HasForeignKey(x => x.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: (TeacherId, DayOfWeekId, TimeSlotId)
            b.HasIndex(x => new { x.TeacherId, x.DayOfWeekId, x.TimeSlotId }).IsUnique();

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => new { x.TeacherId, x.DayOfWeekId });
        }
    }

    internal sealed class TeacherAvailabilityExceptionConfig : IEntityTypeConfiguration<TeacherAvailabilityException>
    {
        public void Configure(EntityTypeBuilder<TeacherAvailabilityException> b)
        {
            b.ToTable("TeacherAvailabilityExceptions");
            b.HasKey(x => x.TeacherAvailabilityExceptionId);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherAvailabilityExceptions)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.TimeSlot)
                .WithMany(ts => ts.TeacherAvailabilityExceptions)
                .HasForeignKey(x => x.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.Reason).HasMaxLength(250);

            b.HasIndex(x => new { x.TeacherId, x.Date, x.TimeSlotId, x.ExceptionType });
        }
    }

    internal sealed class TeacherAreaConfig : IEntityTypeConfiguration<TeacherArea>
    {
        public void Configure(EntityTypeBuilder<TeacherArea> b)
        {
            b.ToTable("TeacherAreas");
            b.HasKey(x => x.TeacherAreaId);

            b.Property(x => x.MaxDistanceKm).HasPrecision(5, 2);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherAreas)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Location)
                .WithMany(l => l.TeacherAreas)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TeacherId, x.LocationId }).IsUnique();
            b.Property(x => x.IsActive).HasDefaultValue(true);
        }
    }

    internal sealed class CourseConfig : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> b)
        {
            b.ToTable("Courses");
            b.HasKey(x => x.CourseId);

            b.Property(x => x.Price).HasPrecision(10, 2);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.Courses)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TeacherId, x.Status });
            b.HasIndex(x => new { x.SubjectId, x.CurriculumId, x.StageId, x.LevelId });

            b.Property(x => x.CanIncludeInPackages).HasDefaultValue(false);
            b.Property(x => x.Status).HasDefaultValue(CourseStatus.Draft);
        }
    }

    internal sealed class CourseSessionConfig : IEntityTypeConfiguration<CourseSession>
    {
        public void Configure(EntityTypeBuilder<CourseSession> b)
        {
            b.ToTable("CourseSessions");
            b.HasKey(x => x.CourseSessionId);

            b.Property(x => x.Title).HasMaxLength(150).IsRequired();

            b.HasOne(x => x.Course)
                .WithMany(c => c.CourseSessions)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.CourseId, x.SessionNumber }).IsUnique();
        }
    }

    internal sealed class CourseEnrollmentRequestConfig : IEntityTypeConfiguration<CourseEnrollmentRequest>
    {
        public void Configure(EntityTypeBuilder<CourseEnrollmentRequest> b)
        {
            b.ToTable("CourseEnrollmentRequests");
            b.HasKey(x => x.CourseEnrollmentRequestId);

            b.Property(x => x.Notes).HasMaxLength(400);

            b.HasOne(x => x.Course)
                .WithMany(c => c.CourseEnrollmentRequests)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.CourseId, x.Status });
            b.HasIndex(x => new { x.RequestedByStudentId, x.Status });
        }
    }

    internal sealed class CourseRequestSelectedAvailabilityConfig : IEntityTypeConfiguration<CourseRequestSelectedAvailability>
    {
        public void Configure(EntityTypeBuilder<CourseRequestSelectedAvailability> b)
        {
            b.ToTable("CourseRequestSelectedAvailabilities");
            b.HasKey(x => x.CourseRequestSelectedAvailabilityId);

            b.HasOne(x => x.CourseEnrollmentRequest)
                .WithMany(r => r.SelectedAvailabilities)
                .HasForeignKey(x => x.CourseEnrollmentRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.TeacherAvailability)
                .WithMany(a => a.CourseRequestSelectedAvailabilities)
                .HasForeignKey(x => x.TeacherAvailabilityId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.CourseEnrollmentRequestId, x.TeacherAvailabilityId }).IsUnique();
        }
    }

    internal sealed class CourseRequestGroupMemberConfig : IEntityTypeConfiguration<CourseRequestGroupMember>
    {
        public void Configure(EntityTypeBuilder<CourseRequestGroupMember> b)
        {
            b.ToTable("CourseRequestGroupMembers");
            b.HasKey(x => x.CourseRequestGroupMemberId);

            b.HasOne(x => x.CourseEnrollmentRequest)
                .WithMany(r => r.GroupMembers)
                .HasForeignKey(x => x.CourseEnrollmentRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.CourseEnrollmentRequestId, x.StudentId }).IsUnique();
            b.HasIndex(x => x.ConfirmationStatus);
        }
    }

    internal sealed class CourseEnrollmentConfig : IEntityTypeConfiguration<CourseEnrollment>
    {
        public void Configure(EntityTypeBuilder<CourseEnrollment> b)
        {
            b.ToTable("CourseEnrollments");
            b.HasKey(x => x.CourseEnrollmentId);

            b.HasOne(x => x.Course)
                .WithMany(c => c.CourseEnrollments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
            b.HasIndex(x => x.EnrollmentStatus);
        }
    }

    internal sealed class CourseScheduleConfig : IEntityTypeConfiguration<CourseSchedule>
    {
        public void Configure(EntityTypeBuilder<CourseSchedule> b)
        {
            b.ToTable("CourseSchedules");
            b.HasKey(x => x.CourseScheduleId);

            b.HasOne(x => x.CourseEnrollment)
                .WithMany(e => e.CourseSchedules)
                .HasForeignKey(x => x.CourseEnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.TeacherAvailability)
                .WithMany(a => a.CourseSchedules)
                .HasForeignKey(x => x.TeacherAvailabilityId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Location)
                .WithMany(l => l.CourseSchedules)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: (CourseEnrollmentId, Date, TeacherAvailabilityId)
            b.HasIndex(x => new { x.CourseEnrollmentId, x.Date, x.TeacherAvailabilityId }).IsUnique();

            b.HasIndex(x => x.Status);

            // Optional DB check constraint idea (SQL Server): InPerson requires LocationId
            // Add in migration if desired:
            // b.ToTable(t => t.HasCheckConstraint("CK_CourseSchedules_LocationRequiredForInPerson",
            //     $"([{nameof(CourseSchedule.TeachingMode)}] <> {(int)TeachingMode.InPerson}) OR ([{nameof(CourseSchedule.LocationId)}] IS NOT NULL)"));
        }
    }

    internal sealed class SessionRequestConfig : IEntityTypeConfiguration<SessionRequest>
    {
        public void Configure(EntityTypeBuilder<SessionRequest> b)
        {
            b.ToTable("SessionRequests");
            b.HasKey(x => x.SessionRequestId);

            b.Property(x => x.Description).HasMaxLength(800);

            b.HasOne(x => x.Location)
                .WithMany(l => l.SessionRequests)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.StudentId, x.Status });
            b.HasIndex(x => new { x.SubjectId, x.CurriculumId, x.LevelId });
        }
    }

    internal sealed class SessionRequestOfferConfig : IEntityTypeConfiguration<SessionRequestOffer>
    {
        public void Configure(EntityTypeBuilder<SessionRequestOffer> b)
        {
            b.ToTable("SessionRequestOffers");
            b.HasKey(x => x.SessionRequestOfferId);

            b.Property(x => x.ProposedPrice).HasPrecision(10, 2);
            b.Property(x => x.ProposedSchedule).HasMaxLength(800);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasOne(x => x.SessionRequest)
                .WithMany(r => r.Offers)
                .HasForeignKey(x => x.SessionRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.SessionRequestOffers)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.SessionRequestId, x.Status });
            b.HasIndex(x => x.TeacherId);
        }
    }

    internal sealed class SessionConfig : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> b)
        {
            b.ToTable("Sessions");
            b.HasKey(x => x.SessionId);

            b.HasOne(x => x.SessionRequest)
                .WithMany(r => r.Sessions)
                .HasForeignKey(x => x.SessionRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TeacherId, x.Status });
            b.HasIndex(x => new { x.StudentId, x.Status });
        }
    }

    internal sealed class ScheduledSessionConfig : IEntityTypeConfiguration<ScheduledSession>
    {
        public void Configure(EntityTypeBuilder<ScheduledSession> b)
        {
            b.ToTable("ScheduledSessions");
            b.HasKey(x => x.ScheduledSessionId);

            b.HasOne(x => x.Session)
                .WithMany(s => s.ScheduledSessions)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.TimeSlot)
                .WithMany()
                .HasForeignKey(x => x.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.SessionId, x.Date, x.TimeSlotId }).IsUnique();
            b.HasIndex(x => x.Status);
        }
    }

    internal sealed class TeacherReviewConfig : IEntityTypeConfiguration<TeacherReview>
    {
        public void Configure(EntityTypeBuilder<TeacherReview> b)
        {
            b.ToTable("TeacherReviews");
            b.HasKey(x => x.TeacherReviewId);

            b.Property(x => x.Feedback).HasMaxLength(600);

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherReviews)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TeacherId, x.StudentId });
        }
    }

    internal sealed class TeacherAuditLogConfig : IEntityTypeConfiguration<TeacherAuditLog>
    {
        public void Configure(EntityTypeBuilder<TeacherAuditLog> b)
        {
            b.ToTable("TeacherAuditLogs");
            b.HasKey(x => x.TeacherAuditLogId);

            b.Property(x => x.Action).HasMaxLength(80).IsRequired();
            b.Property(x => x.TableName).HasMaxLength(80).IsRequired();

            b.HasOne(x => x.Teacher)
                .WithMany(t => t.TeacherAuditLogs)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TeacherId, x.CreatedAt });
        }
    }

    internal sealed class PaymentConfig : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> b)
        {
            b.ToTable("Payments");
            b.HasKey(x => x.PaymentId);

            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.PaymentProvider).HasMaxLength(40).IsRequired();
            b.Property(x => x.ProviderTransactionId).HasMaxLength(120);

            b.Property(x => x.Subtotal).HasPrecision(10, 2);
            b.Property(x => x.VatAmount).HasPrecision(10, 2);
            b.Property(x => x.DiscountAmount).HasPrecision(10, 2);
            b.Property(x => x.TotalAmount).HasPrecision(10, 2);

            b.Property(x => x.InvoiceNumber).HasMaxLength(50);
            b.Property(x => x.ReceiptUrl).HasMaxLength(600);
            b.Property(x => x.ReceiptPath).HasMaxLength(600);

            b.HasIndex(x => new { x.PayerUserId, x.Status, x.CreatedAt });
            b.HasIndex(x => x.ProviderTransactionId);
        }
    }

    internal sealed class PaymentItemConfig : IEntityTypeConfiguration<PaymentItem>
    {
        public void Configure(EntityTypeBuilder<PaymentItem> b)
        {
            b.ToTable("PaymentItems");
            b.HasKey(x => x.PaymentItemId);

            b.Property(x => x.Description).HasMaxLength(200);
            b.Property(x => x.Amount).HasPrecision(10, 2);

            b.HasOne(x => x.Payment)
                .WithMany(p => p.PaymentItems)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.PaymentId);
            b.HasIndex(x => new { x.ItemType, x.ReferenceId });
        }
    }

    internal sealed class CourseEnrollmentPaymentConfig : IEntityTypeConfiguration<CourseEnrollmentPayment>
    {
        public void Configure(EntityTypeBuilder<CourseEnrollmentPayment> b)
        {
            b.ToTable("CourseEnrollmentPayments");
            b.HasKey(x => x.CourseEnrollmentPaymentId);

            b.HasOne(x => x.CourseEnrollment)
                .WithMany(e => e.CourseEnrollmentPayments)
                .HasForeignKey(x => x.CourseEnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Payment)
                .WithMany(p => p.CourseEnrollmentPayments)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.CourseEnrollmentId, x.Status });
            b.HasIndex(x => x.PaymentId);
        }
    }
}
