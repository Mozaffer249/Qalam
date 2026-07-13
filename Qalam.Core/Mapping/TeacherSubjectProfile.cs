using AutoMapper;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Core.Mapping;

public class TeacherSubjectProfile : Profile
{
    public TeacherSubjectProfile()
    {
        // TeacherSubject -> TeacherSubjectResponseDto
        CreateMap<TeacherSubject, TeacherSubjectResponseDto>()
            .ForMember(dest => dest.SubjectNameAr, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.NameAr : ""))
            .ForMember(dest => dest.SubjectNameEn, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.NameEn : ""))
            .ForMember(dest => dest.DomainCode, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Domain != null ? src.Subject.Domain.Code : null))
            .ForMember(dest => dest.DomainNameAr, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Domain != null ? src.Subject.Domain.NameAr : null))
            .ForMember(dest => dest.DomainNameEn, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Domain != null ? src.Subject.Domain.NameEn : null))
            .ForMember(dest => dest.CurriculumId, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.CurriculumId : null))
            .ForMember(dest => dest.CurriculumNameAr, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Curriculum != null ? src.Subject.Curriculum.NameAr : null))
            .ForMember(dest => dest.CurriculumNameEn, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Curriculum != null ? src.Subject.Curriculum.NameEn : null))
            .ForMember(dest => dest.LevelId, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.LevelId : null))
            .ForMember(dest => dest.LevelNameAr, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Level != null ? src.Subject.Level.NameAr : null))
            .ForMember(dest => dest.LevelNameEn, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Level != null ? src.Subject.Level.NameEn : null))
            .ForMember(dest => dest.GradeId, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.GradeId : null))
            .ForMember(dest => dest.GradeNameAr, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Grade != null ? src.Subject.Grade.NameAr : null))
            .ForMember(dest => dest.GradeNameEn, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Grade != null ? src.Subject.Grade.NameEn : null))
            .ForMember(dest => dest.Units, opt => opt.MapFrom(src => src.TeacherSubjectUnits));

        // TeacherSubjectUnit -> TeacherSubjectUnitResponseDto
        CreateMap<TeacherSubjectUnit, TeacherSubjectUnitResponseDto>()
            .ForMember(dest => dest.UnitNameAr, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.NameAr : ""))
            .ForMember(dest => dest.UnitNameEn, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.NameEn : ""))
            .ForMember(dest => dest.UnitTypeCode, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.UnitTypeCode : null))
            .ForMember(dest => dest.QuranContentTypeNameAr, opt => opt.MapFrom(src => src.QuranContentType != null ? src.QuranContentType.NameAr : null))
            .ForMember(dest => dest.QuranContentTypeNameEn, opt => opt.MapFrom(src => src.QuranContentType != null ? src.QuranContentType.NameEn : null))
            .ForMember(dest => dest.QuranLevelNameAr, opt => opt.MapFrom(src => src.QuranLevel != null ? src.QuranLevel.NameAr : null))
            .ForMember(dest => dest.QuranLevelNameEn, opt => opt.MapFrom(src => src.QuranLevel != null ? src.QuranLevel.NameEn : null));
    }
}
