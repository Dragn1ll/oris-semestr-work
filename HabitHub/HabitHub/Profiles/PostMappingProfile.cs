using Application.Dto_s;
using Application.Dto_s.Post;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class PostMappingProfile : Profile
{
    public PostMappingProfile()
    {
        CreateMap<Post, PostEntity>()
            .ForMember(dest => dest.User, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Habit, 
                opt => opt.Ignore())
            .ForMember(dest => dest.MediaFiles, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Comments, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Likes, 
                opt => opt.Ignore());

        CreateMap<PostEntity, Post>()
            .ConstructUsing(src => Post.Create(
                src.Id,
                src.UserId,
                src.HabitId,
                src.Text,
                src.DateTime
            ));

        CreateMap<PostAddDto, Post>()
            .ConstructUsing(src => Post.Create(
                Guid.NewGuid(),
                src.UserId,
                src.HabitId,
                src.Text,
                DateTime.UtcNow
                )
            );

        CreateMap<Post, PostInfoDto>();
        
        CreateMap<Post, IdDto>();
    }
}