using AutoMapper;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using System.Linq;

namespace Qalam.Core.Mapping;

public class CourseProfile : Profile
{
    public CourseProfile()
    {
        // Course -> CourseCatalogItemDto
        CreateMap<Course, CourseCatalogItemDto>()
            .ForMember(dest => dest.TeacherDisplayName, opt => opt.MapFrom(src =>
                src.Teacher != null && src.Teacher.User != null
                    ? (src.Teacher.User.FirstName + " " + src.Teacher.User.LastName).Trim()
                    : null))
            .ForMember(dest => dest.DescriptionShort, opt => opt.MapFrom(src =>
                src.Description != null && src.Description.Length > 200
                    ? src.Description.Substring(0, 200) + "..."
                    : src.Description))
            .ForMember(dest => dest.DomainId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.DomainId
                    : 0))
            .ForMember(dest => dest.DomainNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null && src.TeacherSubject.Subject.Domain != null
                    ? src.TeacherSubject.Subject.Domain.NameEn
                    : null))
            .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null ? src.TeacherSubject.SubjectId : 0))
            .ForMember(dest => dest.SubjectNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.NameEn
                    : null))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.TeachingMode != null ? src.TeachingMode.NameEn : null))
            .ForMember(dest => dest.SessionTypeNameEn, opt => opt.MapFrom(src =>
                src.SessionType != null ? src.SessionType.NameEn : null))
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src =>
                src.MaxStudents.HasValue
                    ? src.MaxStudents.Value - src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                    : (int?)null));

        // Course -> CourseCatalogDetailDto
        CreateMap<Course, CourseCatalogDetailDto>()
            .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherId))
            .ForMember(dest => dest.TeacherDisplayName, opt => opt.MapFrom(src =>
                src.Teacher != null && src.Teacher.User != null
                    ? (src.Teacher.User.FirstName + " " + src.Teacher.User.LastName).Trim()
                    : null))
            .ForMember(dest => dest.DomainId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.DomainId
                    : 0))
            .ForMember(dest => dest.DomainNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null && src.TeacherSubject.Subject.Domain != null
                    ? src.TeacherSubject.Subject.Domain.NameEn
                    : null))
            .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null ? src.TeacherSubject.SubjectId : 0))
            .ForMember(dest => dest.SubjectNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.NameEn
                    : null))
            .ForMember(dest => dest.CurriculumId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.CurriculumId
                    : null))
            .ForMember(dest => dest.CurriculumNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null && src.TeacherSubject.Subject.Curriculum != null
                    ? src.TeacherSubject.Subject.Curriculum.NameEn
                    : null))
            .ForMember(dest => dest.LevelId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.LevelId
                    : null))
            .ForMember(dest => dest.LevelNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null && src.TeacherSubject.Subject.Level != null
                    ? src.TeacherSubject.Subject.Level.NameEn
                    : null))
            .ForMember(dest => dest.GradeId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null
                    ? src.TeacherSubject.Subject.GradeId
                    : null))
            .ForMember(dest => dest.GradeNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Subject != null && src.TeacherSubject.Subject.Grade != null
                    ? src.TeacherSubject.Subject.Grade.NameEn
                    : null))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.TeachingMode != null ? src.TeachingMode.NameEn : null))
            .ForMember(dest => dest.SessionTypeNameEn, opt => opt.MapFrom(src =>
                src.SessionType != null ? src.SessionType.NameEn : null))
            .ForMember(dest => dest.SessionTypeCode, opt => opt.MapFrom(src =>
                src.SessionType != null ? src.SessionType.Code : null))
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src =>
                src.MaxStudents.HasValue
                    ? src.MaxStudents.Value - src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                    : (int?)null))
            .ForMember(dest => dest.Sessions, opt => opt.MapFrom(src =>
                src.Sessions
                    .OrderBy(s => s.SessionNumber)
                    .Select(s => new CourseSessionDto
                    {
                        Id = s.Id,
                        SessionNumber = s.SessionNumber,
                        DurationMinutes = s.DurationMinutes,
                        Title = s.Title,
                        Notes = s.Notes,
                        Units = s.Units.Select(u => new CourseSessionUnitDto
                        {
                            Id = u.Id,
                            ContentUnitId = u.ContentUnitId,
                            ContentUnitNameEn = u.ContentUnit != null ? u.ContentUnit.NameEn : null,
                            ContentUnitNameAr = u.ContentUnit != null ? u.ContentUnit.NameAr : null,
                            LessonId = u.LessonId,
                            LessonNameEn = u.Lesson != null ? u.Lesson.NameEn : null,
                            LessonNameAr = u.Lesson != null ? u.Lesson.NameAr : null
                        }).ToList()
                    })
                    .ToList()));

        // CourseEnrollmentRequest -> EnrollmentRequestListItemDto
        CreateMap<CourseEnrollmentRequest, EnrollmentRequestListItemDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Title : ""))
            .ForMember(dest => dest.CourseImageUrl, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.ImageUrl : null))
            .ForMember(dest => dest.TeacherDisplayName, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.Teacher != null && src.Course.Teacher.User != null
                    ? (src.Course.Teacher.User.FirstName + " " + src.Course.Teacher.User.LastName).Trim()
                    : null))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src =>
                src.Course != null
                && src.Course.TeacherSubject != null
                && src.Course.TeacherSubject.Subject != null
                    ? src.Course.TeacherSubject.Subject.NameAr
                    : null))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.TeachingMode != null
                    ? src.Course.TeachingMode.NameEn
                    : null))
            .ForMember(dest => dest.SessionsCount, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.Sessions != null && src.Course.Sessions.Count > 0
                    ? src.Course.Sessions.Count
                    : (int?)null))
            .ForMember(dest => dest.EstimatedTotalPrice, opt => opt.MapFrom(src => src.EstimatedTotalPrice))
            .ForMember(dest => dest.Kind, opt => opt.Ignore())
            .ForMember(dest => dest.HasPendingInvites, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentId, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentStatus, opt => opt.Ignore());

        // CourseEnrollmentRequest -> EnrollmentRequestDetailDto
        CreateMap<CourseEnrollmentRequest, EnrollmentRequestDetailDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Title : ""))
            .ForMember(dest => dest.CourseDescriptionShort, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.Description != null && src.Course.Description.Length > 150
                    ? src.Course.Description.Substring(0, 150) + "..."
                    : src.Course != null ? src.Course.Description : null))
            .ForMember(dest => dest.CoursePrice, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Price : 0))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.TeachingMode != null
                    ? src.Course.TeachingMode.NameEn
                    : null))
            .ForMember(dest => dest.SessionTypeId, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.SessionTypeId : 0))
            .ForMember(dest => dest.SessionTypeNameEn, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.SessionType != null
                    ? src.Course.SessionType.NameEn
                    : null))
            .ForMember(dest => dest.TotalMinutes, opt => opt.MapFrom(src => src.TotalMinutes))
            .ForMember(dest => dest.EstimatedTotalPrice, opt => opt.MapFrom(src => src.EstimatedTotalPrice))
            .ForMember(dest => dest.SelectedSessionSlots, opt => opt.MapFrom(src =>
                src.SelectedSessionSlots
                    .OrderBy(x => x.SessionNumber)
                    .Select(x => new SelectedSessionSlotDto
                    {
                        SessionNumber = x.SessionNumber,
                        TeacherAvailabilityId = x.TeacherAvailabilityId,
                        Date = x.SessionDate
                    })
                    .ToList()))
            .ForMember(dest => dest.GroupMembers, opt => opt.MapFrom(src =>
                src.GroupMembers.Select(g => new EnrollmentRequestGroupMemberDto
                {
                    StudentId = g.StudentId,
                    StudentName = g.Student != null && g.Student.User != null
                        ? ((g.Student.User.FirstName ?? "") + " " + (g.Student.User.LastName ?? "")).Trim()
                        : null,
                    MemberType = g.MemberType,
                    ConfirmationStatus = g.ConfirmationStatus,
                    ConfirmedAt = g.ConfirmedAt,
                    ConfirmedByUserId = g.ConfirmedByUserId
                }).ToList()))
            .ForMember(dest => dest.ProposedSessions, opt => opt.MapFrom(src =>
                src.ProposedSessions
                    .OrderBy(p => p.SessionNumber)
                    .Select(p => new EnrollmentRequestProposedSessionDto
                    {
                        SessionNumber = p.SessionNumber,
                        DurationMinutes = p.DurationMinutes,
                        Title = p.Title,
                        Notes = p.Notes
                    }).ToList()))
            // ProposedScheduleDates and viewer flags are set by the query handler.
            .ForMember(dest => dest.ProposedScheduleDates, opt => opt.Ignore())
            .ForMember(dest => dest.IsOwner, opt => opt.Ignore())
            .ForMember(dest => dest.Kind, opt => opt.Ignore())
            .ForMember(dest => dest.CanPay, opt => opt.Ignore())
            .ForMember(dest => dest.CanCancelInvite, opt => opt.Ignore())
            .ForMember(dest => dest.CanCancel, opt => opt.Ignore())
            .ForMember(dest => dest.ActionableMemberStudentIds, opt => opt.Ignore())
            .ForMember(dest => dest.ViewerStudentIds, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentId, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentStatus, opt => opt.Ignore())
            .ForMember(dest => dest.AmountDue, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentDeadline, opt => opt.Ignore())
            .ForMember(dest => dest.PayParticipantId, opt => opt.Ignore());

        // Enrollment -> EnrollmentListItemDto
        CreateMap<Enrollment, EnrollmentListItemDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Title : ""))
            .ForMember(dest => dest.CourseImageUrl, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.ImageUrl : null))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src =>
                src.Course != null
                && src.Course.TeacherSubject != null
                && src.Course.TeacherSubject.Subject != null
                    ? src.Course.TeacherSubject.Subject.NameAr
                    : null))
            .ForMember(dest => dest.TeacherDisplayName, opt => opt.MapFrom(src =>
                src.ApprovedByTeacher != null && src.ApprovedByTeacher.User != null
                    ? (src.ApprovedByTeacher.User.FirstName + " " + src.ApprovedByTeacher.User.LastName).Trim()
                    : null))
            .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count))
            .ForMember(dest => dest.LeaderStudentName, opt => opt.MapFrom(src =>
                src.LeaderStudent != null && src.LeaderStudent.User != null
                    ? ((src.LeaderStudent.User.FirstName ?? "") + " " + (src.LeaderStudent.User.LastName ?? "")).Trim()
                    : null))
            .ForMember(dest => dest.SessionsCount, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.Sessions != null && src.Course.Sessions.Count > 0
                    ? src.Course.Sessions.Count
                    : (int?)null));

        // EnrollmentParticipant -> EnrollmentParticipantDto
        CreateMap<EnrollmentParticipant, EnrollmentParticipantDto>()
            .ForMember(dest => dest.StudentFullName, opt => opt.MapFrom(src =>
                src.Student != null && src.Student.User != null
                    ? ((src.Student.User.FirstName ?? "") + " " + (src.Student.User.LastName ?? "")).Trim()
                    : null));

        // Enrollment -> EnrollmentDetailDto (Sessions list is composed by the handler from CourseSchedules)
        CreateMap<Enrollment, EnrollmentDetailDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Title : ""))
            .ForMember(dest => dest.CourseDescription, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Description : null))
            .ForMember(dest => dest.CoursePrice, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Price : 0))
            .ForMember(dest => dest.TeacherDisplayName, opt => opt.MapFrom(src =>
                src.ApprovedByTeacher != null && src.ApprovedByTeacher.User != null
                    ? (src.ApprovedByTeacher.User.FirstName + " " + src.ApprovedByTeacher.User.LastName).Trim()
                    : null))
            .ForMember(dest => dest.TeachingModeId, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.TeachingModeId : 0))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.TeachingMode != null
                    ? src.Course.TeachingMode.NameEn
                    : null))
            .ForMember(dest => dest.SessionTypeId, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.SessionTypeId : 0))
            .ForMember(dest => dest.SessionTypeNameEn, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.SessionType != null
                    ? src.Course.SessionType.NameEn
                    : null))
            .ForMember(dest => dest.Sessions, opt => opt.Ignore())
            .ForMember(dest => dest.IsOwner, opt => opt.Ignore())
            .ForMember(dest => dest.CanPay, opt => opt.Ignore())
            .ForMember(dest => dest.CanCancel, opt => opt.Ignore())
            .ForMember(dest => dest.PayParticipantId, opt => opt.Ignore());
    }
}
