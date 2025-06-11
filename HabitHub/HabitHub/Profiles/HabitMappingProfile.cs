using Application.Dto_s;
using Application.Dto_s.Habit;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class HabitMappingProfile : Profile
{
    public HabitMappingProfile()
    {
        CreateMap<Habit, HabitEntity>()
            .ForMember(dest => dest.User, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Posts, 
                opt => opt.Ignore())
            .ForMember(dest => dest.HabitProgress,
                opt => opt.Ignore());
        
        CreateMap<HabitEntity, Habit>()
            .ConstructUsing(src =>
                Habit.Create(
                    src.Id,
                    src.UserId,
                    src.Type,
                    src.PhysicalActivityType,
                    src.Goal,
                    src.IsActive
                )
            );

        CreateMap<HabitAddDto, Habit>()
            .ConstructUsing(src =>
                Habit.Create(
                    Guid.NewGuid(),
                    src.UserId,
                    src.Type,
                    src.PhysicalActivityType,
                    src.Goal,
                    true
                    )
                );
        
        CreateMap<Habit, HabitInfoDto>();

        CreateMap<Habit, IdDto>();
    }
}