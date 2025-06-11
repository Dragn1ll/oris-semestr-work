using Application.Dto_s;
using Application.Dto_s.User;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services.MainServices;

public class UserService(
    IUserRepository userRepository,
    IMapper mapper,
    ILogger<UserService> logger
    ) : IUserService
{
    public async Task<Result<IdDto>> AddAsync(UserAddDto userAddDto)
    {
        try
        {
            var getResult = await userRepository.GetByFilterAsync(u => u.Email == userAddDto.Email);
            if (getResult is { IsSuccess: true, Value: not null })
                return Result<IdDto>.Failure(new Error(ErrorType.BadRequest,
                    "Пользователь с таким Email уже существует"));
            
            var userModel = mapper.Map<User>(userAddDto);
            var addResult = await userRepository.AddAsync(userModel);

            return !addResult.IsSuccess 
                ? Result<IdDto>.Failure(addResult.Error) 
                : Result<IdDto>.Success(mapper.Map<IdDto>(userModel));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании пользователя");
            return Result<IdDto>.Failure(new Error(
                ErrorType.ServerError,
                "Не удалось создать пользователя"));
        }
    }

    public async Task<Result<UserInfoDto>> GetByIdAsync(Guid userId)
    {
        try
        {
            var getResult = await userRepository.GetByFilterAsync(x => x.Id == userId);
            
            if (getResult is { IsSuccess: true, Value: null })
                return Result<UserInfoDto>.Failure(new Error(ErrorType.NotFound, 
                    "Пользователь не найден"));

            return !getResult.IsSuccess 
                ? Result<UserInfoDto>.Failure(getResult.Error) 
                : Result<UserInfoDto>.Success(mapper.Map<UserInfoDto>(getResult.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при получении пользователя по Id: {userId}");
            return Result<UserInfoDto>.Failure(new Error(
                ErrorType.ServerError,
                "Не удалось найти пользователя"));
        }
    }

    public async Task<Result<UserAuthInfoDto>> GetUserAuthInfoAsync(string email)
    {
        try
        {
            var getResult = await userRepository.GetByFilterAsync(x => x.Email == email);
            
            if (getResult is { IsSuccess: true, Value: null })
                return Result<UserAuthInfoDto>.Failure(new Error(ErrorType.NotFound, 
                    "Пользователь не найден"));
            
            return !getResult.IsSuccess 
                ? Result<UserAuthInfoDto>.Failure(getResult.Error) 
                : Result<UserAuthInfoDto>.Success(mapper.Map<UserAuthInfoDto>(getResult.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при получении данных пользователя по email: {email}");
            return Result<UserAuthInfoDto>.Failure(new Error(
                ErrorType.ServerError,
                "Не удалось получить данные пользователя"));
        }
    }

    public async Task<Result> UpdateByIdAsync(Guid userId, UserInfoDto userInfoDto)
    {
        try
        {
            var updateResult = await userRepository.UpdateAsync(userId, u =>
            {
                u.Name = userInfoDto.Name;
                u.Surname = userInfoDto.Surname;
                u.Patronymic = userInfoDto.Patronymic;
                u.Status = userInfoDto.Status;
                u.BirthDay = userInfoDto.BirthDay;
            });

            return updateResult.IsSuccess
                ? Result.Success()
                : Result.Failure(updateResult.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при обновлении пользователя с Id: {userId}");
            return Result.Failure(new Error(
                ErrorType.ServerError,
                "Не удалось обновить данные пользователя"));
        }
    }

    public async Task<Result> DeleteByIdAsync(Guid userId)
    {
        try
        {
            var deleteResult = await userRepository.DeleteAsync(x => x.Id == userId);

            return deleteResult.IsSuccess
                ? Result.Success()
                : Result.Failure(deleteResult.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при удалении пользователя с Id: {userId}");
            return Result.Failure(new Error(
                ErrorType.ServerError,
                "Не удалось удалить пользователя"));
        }
    }
}