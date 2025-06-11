using Application.Dto_s;
using Application.Dto_s.User;
using Application.Utils;

namespace Application.Interfaces.Services.MainServices;

public interface IUserService
{
    Task<Result<IdDto>> AddAsync(UserAddDto userAddDto);
    Task<Result<UserInfoDto>> GetByIdAsync(Guid userId);
    Task<Result<UserAuthInfoDto>> GetUserAuthInfoAsync(string email);
    Task<Result> UpdateByIdAsync(Guid userId, UserInfoDto userInfoDto);
    Task<Result> DeleteByIdAsync(Guid userId);
}