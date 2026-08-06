using AutoMapper;
using Qalam.Data.Commons;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Core.Mapping;

public class OpenSessionRequestProfile : Profile
{
    public OpenSessionRequestProfile()
    {
        // Detail — full DTO returned by Create + GetById
        CreateMap<OpenSessionRequest, OpenSessionRequestDetailDto>()
            .ForMember(d => d.StudentName, opt => opt.MapFrom(s =>
                s.Student != null && s.Student.User != null
                    ? (s.Student.User.FirstName + " " + s.Student.User.LastName).Trim()
                    : null))
            .ForMember(d => d.CreatedByGuardianName, opt => opt.MapFrom(s =>
                s.CreatedByGuardian != null
                    ? (s.CreatedByGuardian.User != null
                        ? (s.CreatedByGuardian.User.FirstName + " " + s.CreatedByGuardian.User.LastName).Trim()
                        : s.CreatedByGuardian.FullName)
                    : null))
            .ForMember(d => d.DomainName, opt => opt.MapFrom(s =>
                s.Domain != null
                    ? LocalizableEntity.GetLocalizedValue(s.Domain.NameAr, s.Domain.NameEn)
                    : null))
            .ForMember(d => d.SubjectName, opt => opt.MapFrom(s =>
                s.Subject != null
                    ? LocalizableEntity.GetLocalizedValue(s.Subject.NameAr, s.Subject.NameEn)
                    : null))
            .ForMember(d => d.CurriculumName, opt => opt.MapFrom(s =>
                s.Curriculum != null
                    ? LocalizableEntity.GetLocalizedValue(s.Curriculum.NameAr, s.Curriculum.NameEn)
                    : null))
            .ForMember(d => d.LevelName, opt => opt.MapFrom(s =>
                s.Level != null
                    ? LocalizableEntity.GetLocalizedValue(s.Level.NameAr, s.Level.NameEn)
                    : null))
            .ForMember(d => d.GradeName, opt => opt.MapFrom(s =>
                s.Grade != null
                    ? LocalizableEntity.GetLocalizedValue(s.Grade.NameAr, s.Grade.NameEn)
                    : null))
            .ForMember(d => d.TermName, opt => opt.MapFrom(s =>
                s.Term != null
                    ? LocalizableEntity.GetLocalizedValue(s.Term.NameAr, s.Term.NameEn)
                    : null))
            .ForMember(d => d.UniversityName, opt => opt.MapFrom(s =>
                s.University != null
                    ? LocalizableEntity.GetLocalizedValue(s.University.NameAr, s.University.NameEn)
                    : null))
            .ForMember(d => d.CollegeName, opt => opt.MapFrom(s =>
                s.College != null
                    ? LocalizableEntity.GetLocalizedValue(s.College.NameAr, s.College.NameEn)
                    : null))
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s =>
                s.Department != null
                    ? LocalizableEntity.GetLocalizedValue(s.Department.NameAr, s.Department.NameEn)
                    : null))
            .ForMember(d => d.AcademicProgramName, opt => opt.MapFrom(s =>
                s.AcademicProgram != null
                    ? LocalizableEntity.GetLocalizedValue(s.AcademicProgram.NameAr, s.AcademicProgram.NameEn)
                    : null))
            .ForMember(d => d.TeachingModeName, opt => opt.MapFrom(s =>
                s.TeachingMode != null
                    ? LocalizableEntity.GetLocalizedValue(s.TeachingMode.NameAr, s.TeachingMode.NameEn)
                    : null))
            .ForMember(d => d.TargetedTeacherName, opt => opt.MapFrom(s =>
                s.TargetedTeacher != null && s.TargetedTeacher.User != null
                    ? (s.TargetedTeacher.User.FirstName + " " + s.TargetedTeacher.User.LastName).Trim()
                    : null))
            .ForMember(d => d.Sessions, opt => opt.MapFrom(s => s.Sessions.OrderBy(x => x.SequenceNumber)))
            .ForMember(d => d.Invitations, opt => opt.MapFrom(s => s.Invitations))
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s => s.Attachments))
            .ForMember(d => d.OffersCount, opt => opt.MapFrom(s => s.Offers.Count));

        CreateMap<OpenSessionRequestSession, OpenSessionRequestSessionDto>()
            .ForMember(d => d.QuranContentTypeName, opt => opt.MapFrom(s =>
                s.QuranContentType != null
                    ? LocalizableEntity.GetLocalizedValue(s.QuranContentType.NameAr, s.QuranContentType.NameEn)
                    : null))
            .ForMember(d => d.QuranLevelName, opt => opt.MapFrom(s =>
                s.QuranLevel != null
                    ? LocalizableEntity.GetLocalizedValue(s.QuranLevel.NameAr, s.QuranLevel.NameEn)
                    : null));

        CreateMap<OpenSessionRequestSessionUnit, OpenSessionRequestUnitDto>()
            .ForMember(d => d.ContentUnitNameEn, opt => opt.MapFrom(s =>
                s.ContentUnit != null ? s.ContentUnit.NameEn : null))
            .ForMember(d => d.ContentUnitNameAr, opt => opt.MapFrom(s =>
                s.ContentUnit != null ? s.ContentUnit.NameAr : null))
            .ForMember(d => d.LessonNameEn, opt => opt.MapFrom(s =>
                s.Lesson != null ? s.Lesson.NameEn : null))
            .ForMember(d => d.LessonNameAr, opt => opt.MapFrom(s =>
                s.Lesson != null ? s.Lesson.NameAr : null));
        CreateMap<OpenSessionRequestAttachment, OpenSessionRequestAttachmentDto>();

        CreateMap<OpenSessionRequestInvitation, OpenSessionRequestInvitationDto>()
            .ForMember(d => d.InvitedStudentName, opt => opt.MapFrom(s =>
                s.InvitedStudent != null && s.InvitedStudent.User != null
                    ? (s.InvitedStudent.User.FirstName + " " + s.InvitedStudent.User.LastName).Trim()
                    : null));

        // List item — flat shape for GET /my
        CreateMap<OpenSessionRequest, OpenSessionRequestListItemDto>()
            .ForMember(d => d.StudentName, opt => opt.MapFrom(s =>
                s.Student != null && s.Student.User != null
                    ? (s.Student.User.FirstName + " " + s.Student.User.LastName).Trim()
                    : null))
            .ForMember(d => d.SubjectName, opt => opt.MapFrom(s =>
                s.Subject != null
                    ? LocalizableEntity.GetLocalizedValue(s.Subject.NameAr, s.Subject.NameEn)
                    : null))
            .ForMember(d => d.TeachingModeName, opt => opt.MapFrom(s =>
                s.TeachingMode != null
                    ? LocalizableEntity.GetLocalizedValue(s.TeachingMode.NameAr, s.TeachingMode.NameEn)
                    : null))
            .ForMember(d => d.TargetedTeacherName, opt => opt.MapFrom(s =>
                s.TargetedTeacher != null && s.TargetedTeacher.User != null
                    ? (s.TargetedTeacher.User.FirstName + " " + s.TargetedTeacher.User.LastName).Trim()
                    : null))
            .ForMember(d => d.OffersCount, opt => opt.MapFrom(s => s.Offers.Count));
    }
}
