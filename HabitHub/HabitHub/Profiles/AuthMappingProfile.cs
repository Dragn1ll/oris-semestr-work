using Application.Dto_s.User;
using AutoMapper;
using HabitHub.Requests.Auth;

namespace HabitHub.Profiles;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<RegisterRequest, UserAddDto>()
            .ForMember(dest => dest.PasswordHash, 
                opt => opt.Ignore());
    }
}