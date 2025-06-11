using Application.Dto_s.Fit;
using Application.Dto_s.Habit;
using AutoMapper;
using Domain.Entities;
using Domain.Models;
using HabitHub.Requests.Google;

namespace HabitHub.Profiles;

public class HabitProgressMappingProfile : Profile
{
    public HabitProgressMappingProfile()
    {
        CreateMap<HabitProgress, HabitProgressEntity>()
            .ForMember(dest => dest.Habit, 
                opt => opt.Ignore());

        CreateMap<HabitProgressEntity, HabitProgress>()
            .ConstructUsing(src => HabitProgress.Create(
                src.Id,
                src.HabitId,
                src.Date,
                src.PercentageCompletion
                )
            );

        CreateMap<HabitProgressAddDto, HabitProgress>()
            .ConstructUsing(src => HabitProgress.Create(
                Guid.NewGuid(), 
                src.HabitId,
                src.Date,
                src.PercentageCompletion
                )
            );

        CreateMap<HabitProgress, HabitProgressInfoDto>();

        CreateMap<FitAnalyzeRequest, FitAnalyzeDto>();
    }
}