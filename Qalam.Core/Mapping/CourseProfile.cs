using AutoMapper;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

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
                    ? src.MaxStudents.Value - src.CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                    : (int?)null));

        // Course -> CourseCatalogDetailDto
        CreateMap<Course, CourseCatalogDetailDto>()
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
                src.TeacherSubject != null ? src.TeacherSubject.CurriculumId : null))
            .ForMember(dest => dest.CurriculumNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Curriculum != null
                    ? src.TeacherSubject.Curriculum.NameEn
                    : null))
            .ForMember(dest => dest.LevelId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null ? src.TeacherSubject.LevelId : null))
            .ForMember(dest => dest.LevelNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Level != null
                    ? src.TeacherSubject.Level.NameEn
                    : null))
            .ForMember(dest => dest.GradeId, opt => opt.MapFrom(src =>
                src.TeacherSubject != null ? src.TeacherSubject.GradeId : null))
            .ForMember(dest => dest.GradeNameEn, opt => opt.MapFrom(src =>
                src.TeacherSubject != null && src.TeacherSubject.Grade != null
                    ? src.TeacherSubject.Grade.NameEn
                    : null))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.TeachingMode != null ? src.TeachingMode.NameEn : null))
            .ForMember(dest => dest.SessionTypeNameEn, opt => opt.MapFrom(src =>
                src.SessionType != null ? src.SessionType.NameEn : null))
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src =>
                src.MaxStudents.HasValue
                    ? src.MaxStudents.Value - src.CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                    : (int?)null));

        // CourseEnrollmentRequest -> EnrollmentRequestListItemDto
        CreateMap<CourseEnrollmentRequest, EnrollmentRequestListItemDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Title : ""))
            .ForMember(dest => dest.TeachingModeNameEn, opt => opt.MapFrom(src =>
                src.Course != null && src.Course.TeachingMode != null
                    ? src.Course.TeachingMode.NameEn
                    : null));

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
                    : null));

        // CourseEnrollment -> EnrollmentListItemDto
        CreateMap<CourseEnrollment, EnrollmentListItemDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src =>
                src.Course != null ? src.Course.Title : ""))
            .ForMember(dest => dest.TeacherDisplayName, opt => opt.MapFrom(src =>
                src.ApprovedByTeacher != null && src.ApprovedByTeacher.User != null
                    ? (src.ApprovedByTeacher.User.FirstName + " " + src.ApprovedByTeacher.User.LastName).Trim()
                    : null));

        // CourseEnrollment -> EnrollmentDetailDto
        CreateMap<CourseEnrollment, EnrollmentDetailDto>()
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
                    : null));
    }
}
