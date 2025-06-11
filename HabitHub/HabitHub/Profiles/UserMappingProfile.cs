using Application.Dto_s;
using Application.Dto_s.User;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserEntity>()
            .ForMember(dest => dest.Habits, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Posts, 
                opt => opt.Ignore())
            .ForMember(dest => dest.ReceivedMessages, 
                opt => opt.Ignore())
            .ForMember(dest => dest.SentMessages, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Comments, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Likes, 
                opt => opt.Ignore());

        CreateMap<UserEntity, User>()
            .ConstructUsing(e => User.Create(
                e.Id,
                e.Name,
                e.Surname,
                e.Patronymic,
                e.Email,
                e.PasswordHash,
                e.Status,
                e.BirthDay
            ));
        
        CreateMap<UserAddDto, User>()
            .ConstructUsing(u => User.Create(
                Guid.NewGuid(),
                u.Name,
                u.Surname,
                u.Patronymic,
                u.Email,
                u.PasswordHash,
                u.Status,
                u.BirthDay
            ));
        
        CreateMap<User, IdDto>();
        
        CreateMap<User, UserAuthInfoDto>();
        
        CreateMap<User, UserInfoDto>();
    }
}