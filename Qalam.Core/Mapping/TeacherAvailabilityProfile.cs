using AutoMapper;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Core.Mapping;

public class TeacherAvailabilityProfile : Profile
{
    public TeacherAvailabilityProfile()
    {
        // TeacherAvailability -> TimeSlotResponseDto
        CreateMap<TeacherAvailability, TimeSlotResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TimeSlot.Id))
            .ForMember(dest => dest.AvailabilityId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.TimeSlot.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.TimeSlot.EndTime))
            .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => src.TimeSlot.DurationMinutes))
            .ForMember(dest => dest.LabelAr, opt => opt.MapFrom(src => src.TimeSlot.LabelAr))
            .ForMember(dest => dest.LabelEn, opt => opt.MapFrom(src => src.TimeSlot.LabelEn));

        // TimeSlot -> TimeSlotResponseDto (for exceptions)
        CreateMap<Data.Entity.Common.TimeSlot, TimeSlotResponseDto>()
            .ForMember(dest => dest.AvailabilityId, opt => opt.Ignore());

        // TeacherAvailabilityException -> AvailabilityExceptionResponseDto
        CreateMap<TeacherAvailabilityException, AvailabilityExceptionResponseDto>()
            .ForMember(dest => dest.TimeSlot, opt => opt.MapFrom(src => src.TimeSlot))
            .ForMember(dest => dest.ExceptionTypeDisplay, opt => opt.MapFrom(src => src.ExceptionType.ToString()));
    }
}
