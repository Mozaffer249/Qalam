using AutoMapper;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Student;

namespace Qalam.Core.Mapping;

public class StudentProfile : Profile
{
    public StudentProfile()
    {
        // Student -> ChildStudentDto
        CreateMap<Student, ChildStudentDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => 
                src.User != null 
                    ? (src.User.FirstName + " " + src.User.LastName).Trim() 
                    : ""))
            .ForMember(dest => dest.DomainNameEn, opt => opt.MapFrom(src => 
                src.Domain != null ? src.Domain.NameEn : null))
            .ForMember(dest => dest.CurriculumNameEn, opt => opt.MapFrom(src => 
                src.Curriculum != null ? src.Curriculum.NameEn : null))
            .ForMember(dest => dest.LevelNameEn, opt => opt.MapFrom(src => 
                src.Level != null ? src.Level.NameEn : null))
            .ForMember(dest => dest.GradeNameEn, opt => opt.MapFrom(src => 
                src.Grade != null ? src.Grade.NameEn : null));

        // AddChildDto -> Student (for AddChild command)
        CreateMap<AddChildDto, Student>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.GuardianId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsMinor, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Domain, opt => opt.Ignore())
            .ForMember(dest => dest.Curriculum, opt => opt.Ignore())
            .ForMember(dest => dest.Level, opt => opt.Ignore())
            .ForMember(dest => dest.Grade, opt => opt.Ignore())
            .ForMember(dest => dest.Guardian, opt => opt.Ignore())
            .ForMember(dest => dest.Bio, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}
