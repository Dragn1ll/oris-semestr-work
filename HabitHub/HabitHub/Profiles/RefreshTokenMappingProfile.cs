using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class RefreshTokenMappingProfile : Profile
{
    public RefreshTokenMappingProfile()
    {
        CreateMap<RefreshToken, RefreshTokenEntity>()
            .ForMember(dest => dest.User, 
                opt => opt.Ignore());

        CreateMap<RefreshTokenEntity, RefreshToken>()
            .ConstructUsing(src => RefreshToken.Create(
                src.Id,
                src.UserId,
                src.Token,
                src.Expires,
                src.IsRevoked
                )
            );
    }
}