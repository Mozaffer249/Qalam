using AutoMapper;
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
            .ForMember(d => d.DomainName, opt => opt.MapFrom(s => s.Domain != null ? s.Domain.NameAr : null))
            .ForMember(d => d.SubjectName, opt => opt.MapFrom(s => s.Subject != null ? s.Subject.NameAr : null))
            .ForMember(d => d.TeachingModeName, opt => opt.MapFrom(s => s.TeachingMode != null ? s.TeachingMode.NameAr : null))
            .ForMember(d => d.TargetedTeacherName, opt => opt.MapFrom(s =>
                s.TargetedTeacher != null && s.TargetedTeacher.User != null
                    ? (s.TargetedTeacher.User.FirstName + " " + s.TargetedTeacher.User.LastName).Trim()
                    : null))
            .ForMember(d => d.Sessions, opt => opt.MapFrom(s => s.Sessions.OrderBy(x => x.SequenceNumber)))
            .ForMember(d => d.Invitations, opt => opt.MapFrom(s => s.Invitations))
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s => s.Attachments))
            .ForMember(d => d.OffersCount, opt => opt.MapFrom(s => s.Offers.Count));

        CreateMap<OpenSessionRequestSession, OpenSessionRequestSessionDto>();
        CreateMap<OpenSessionRequestSessionUnit, OpenSessionRequestUnitDto>();
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
            .ForMember(d => d.SubjectName, opt => opt.MapFrom(s => s.Subject != null ? s.Subject.NameAr : null))
            .ForMember(d => d.TeachingModeName, opt => opt.MapFrom(s =>
                s.TeachingMode != null ? s.TeachingMode.NameAr : null))
            .ForMember(d => d.TargetedTeacherName, opt => opt.MapFrom(s =>
                s.TargetedTeacher != null && s.TargetedTeacher.User != null
                    ? (s.TargetedTeacher.User.FirstName + " " + s.TargetedTeacher.User.LastName).Trim()
                    : null))
            .ForMember(d => d.OffersCount, opt => opt.MapFrom(s => s.Offers.Count));
    }
}
