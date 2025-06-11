using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class LikeMappingProfile : Profile
{
    public LikeMappingProfile()
    {
        CreateMap<Like, LikeEntity>()
            .ForMember(dest => dest.User, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Post, 
                opt => opt.Ignore());

        CreateMap<LikeEntity, Like>()
            .ConstructUsing(src => Like.Create(
                src.Id,
                src.UserId,
                src.PostId
            ));
    }
}