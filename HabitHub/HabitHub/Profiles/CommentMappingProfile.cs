using Application.Dto_s.Post;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class CommentMappingProfile : Profile
{
    public CommentMappingProfile()
    {
        CreateMap<Comment, CommentEntity>()
            .ForMember(dest => dest.User, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Post, 
                opt => opt.Ignore());

        CreateMap<CommentEntity, Comment>()
            .ConstructUsing(src => Comment.Create(
                src.Id,
                src.UserId,
                src.PostId,
                src.Text,
                src.DateTime
            ));

        CreateMap<Comment, CommentInfoDto>();
    }
}