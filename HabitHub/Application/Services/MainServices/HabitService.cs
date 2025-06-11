using Application.Dto_s;
using Application.Dto_s.Habit;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services.MainServices;

public class HabitService(
    IHabitRepository habitRepository,
    IHabitProgressRepository habitProgressRepository,
    IMapper mapper,
    ILogger<HabitService> logger) : IHabitService
{
    public async Task<Result<IdDto>> AddAsync(HabitAddDto habitAddDto)
    {
        try
        {
            var habitModel = mapper.Map<Habit>(habitAddDto);
            var addResult = await habitRepository.AddAsync(habitModel);

            return !addResult.IsSuccess
                ? Result<IdDto>.Failure(addResult.Error)
                : Result<IdDto>.Success(mapper.Map<IdDto>(habitModel));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании привычки");
            return Result<IdDto>.Failure(new Error(ErrorType.ServerError,
                $"Не удалось создать привычку: {ex.Message}"));
        }
    }

    public async Task<Result<IdDto>> AddProgressAsync(Guid userId, HabitProgressAddDto habitProgressAddDto)
    {
        try
        {
            if (!await ThereArePowerHabit(userId, habitProgressAddDto.HabitId))
                return Result<IdDto>.Failure(new Error(ErrorType.BadRequest,
                    "У пользователя нет прав для добавления прогресса по этой привычке"));

            var getResult = await habitProgressRepository.GetByFilterAsync(hp =>
                hp.HabitId == habitProgressAddDto.HabitId && hp.Date == habitProgressAddDto.Date);

            if (!getResult.IsSuccess)
                return Result<IdDto>.Failure(getResult.Error);

            if (getResult.Value == null)
            {
                var addResult = await habitProgressRepository.AddAsync(
                    mapper.Map<HabitProgress>(habitProgressAddDto));
                if (!addResult.IsSuccess)
                    return Result<IdDto>.Failure(addResult.Error);
            }
            else
            {
                var putResult = await habitProgressRepository.UpdateAsync(getResult.Value.Id, hp =>
                {
                    hp.PercentageCompletion = habitProgressAddDto.PercentageCompletion;
                });

                if (!putResult.IsSuccess)
                    return Result<IdDto>.Failure(putResult.Error);
            }

            return Result<IdDto>.Success(mapper.Map<IdDto>(getResult.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении прогресса привычки");
            return Result<IdDto>.Failure(new Error(ErrorType.ServerError,
                "Не удалось добавить данные о прогрессе"));
        }
    }

    public async Task<Result<HabitInfoDto>> GetByIdAsync(Guid habitId)
    {
        try
        {
            var getResult = await habitRepository.GetByFilterAsync(h => h.Id == habitId);

            return !getResult.IsSuccess
                ? Result<HabitInfoDto>.Failure(getResult.Error)
                : Result<HabitInfoDto>.Success(mapper.Map<HabitInfoDto>(getResult.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении привычки по ID: {HabitId}", habitId);
            return Result<HabitInfoDto>.Failure(new Error(ErrorType.ServerError,
                "Не удалось получить данные о привычке"));
        }
    }

    public async Task<Result<IEnumerable<HabitInfoDto>>> GetAllByUserIdAsync(Guid userId)
    {
        try
        {
            var getResult = await habitRepository.GetAllByFilterAsync(h => h.UserId == userId);

            return !getResult.IsSuccess
                ? Result<IEnumerable<HabitInfoDto>>.Failure(getResult.Error)
                : Result<IEnumerable<HabitInfoDto>>.Success(mapper.Map<IEnumerable<HabitInfoDto>>(getResult.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при получении привычек пользователя: {userId}");
            return Result<IEnumerable<HabitInfoDto>>.Failure(new Error(ErrorType.ServerError,
                "Не удалось получить данные о привычках пользователя"));
        }
    }

    public async Task<Result<IEnumerable<HabitProgressInfoDto>>> GetAllProgressByHabitIdAsync(Guid habitId)
    {
        try
        {
            var getResult = await habitProgressRepository.GetAllByFilterAsync(h => h.HabitId == habitId);

            return !getResult.IsSuccess
                ? Result<IEnumerable<HabitProgressInfoDto>>.Failure(getResult.Error)
                : Result<IEnumerable<HabitProgressInfoDto>>.Success(
                    mapper.Map<IEnumerable<HabitProgressInfoDto>>(getResult.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при получении прогресса по привычке: {habitId}");
            return Result<IEnumerable<HabitProgressInfoDto>>.Failure(new Error(ErrorType.ServerError,
                "Не удалось получить данные о прогрессе привычки"));
        }
    }

    public async Task<Result> UpdateByIdAsync(Guid userId, HabitInfoDto habitInfoDto)
    {
        try
        {
            if (!await ThereArePowerHabit(userId, habitInfoDto.Id))
                return Result.Failure(new Error(ErrorType.BadRequest,
                    "У пользователя нет прав для редактирования привычки"));

            var updateResult = await habitRepository.UpdateAsync(habitInfoDto.Id, h =>
            {
                h.Type = habitInfoDto.Type;
                h.PhysicalActivityType = habitInfoDto.PhysicalActivityType;
                h.Goal = habitInfoDto.Goal;
                h.IsActive = habitInfoDto.IsActive;
            });

            return !updateResult.IsSuccess ? updateResult : Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при обновлении привычки: {habitInfoDto.Id}");
            return Result.Failure(new Error(ErrorType.ServerError,
                "Не удалось обновить данные привычки"));
        }
    }

    public async Task<Result> DeleteByIdAsync(Guid userId, Guid habitId)
    {
        try
        {
            if (!await ThereArePowerHabit(userId, habitId))
                return Result.Failure(new Error(ErrorType.BadRequest,
                    "У пользователя нет прав для удаления привычки"));

            var deleteResult = await habitRepository.DeleteAsync(h => h.Id == habitId);

            return !deleteResult.IsSuccess ? deleteResult : Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при удалении привычки: {habitId}");
            return Result.Failure(new Error(ErrorType.ServerError,
                "Не удалось удалить привычку"));
        }
    }

    private async Task<bool> ThereArePowerHabit(Guid userId, Guid habitId)
    {
        var getResult = await habitRepository.GetByFilterAsync(h => h.Id == habitId 
                                                                    && h.UserId == userId);
        return getResult is { IsSuccess: true, Value: not null };
    }
}