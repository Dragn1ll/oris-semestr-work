using Application.Dto_s.Fit;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.HelperServices;
using Application.Interfaces.Services.MainServices;
using Application.Utils;
using Domain.Enums;

namespace Application.Services.MainServices;

public class GoogleService(
    IGoogleTokenStore tokenStore, 
    IGoogleFitService fitService, 
    IAiService aiService,
    IHabitRepository habitRepository) : IGoogleService
{
    public async Task<Result> AddGoogleToken(Guid userId, string accessToken, 
        string? refreshToken, DateTime expiresAt)
    {
        try
        {
            var storeResult = await tokenStore.StoreTokenAsync(userId, accessToken, refreshToken, expiresAt);

            return storeResult.IsSuccess
                ? Result.Success()
                : Result.Failure(storeResult.Error);
        }
        catch (Exception)
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                "Не удалось добавить токен в систему"));
        }
    }

    public async Task<Result> RemoveGoogleToken(Guid userId)
    {
        try
        {
            var removeResult = await tokenStore.RemoveTokenAsync(userId);
            
            return removeResult.IsSuccess
                ? Result.Success()
                : Result.Failure(removeResult.Error);
        }
        catch (Exception)
        {
            return Result.Failure(new Error(ErrorType.ServerError,
                "Не удалось удалить токен из системы"));
        }
    }

    public async Task<Result<bool>> HasTokenAsync(Guid userId)
    {
        try
        {
            var hasResult = await tokenStore.HasTokenAsync(userId);
            
            return hasResult.IsSuccess
                ? Result<bool>.Success(hasResult.Value)
                : Result<bool>.Failure(hasResult.Error); 
        }
        catch (Exception)
        {
            return Result<bool>.Failure(new Error(ErrorType.ServerError, 
                "Не удалось проверить наличие токена"));
        }
    }

    public async Task<Result<GoalAnalysisDto>> GetUserFitProgressAsync(Guid userId, FitAnalyzeDto analyzeDto)
    {
        try
        {
            var activityData = await fitService.GetActivityDataAsync(userId, 
                analyzeDto.FromDate, analyzeDto.ToDate);
            
            if (!activityData.IsSuccess)
                return Result<GoalAnalysisDto>.Failure(activityData.Error);

            var habitResult = await habitRepository.GetByFilterAsync(h => h.Id == analyzeDto.HabitId);
            if (!habitResult.IsSuccess)
                return Result<GoalAnalysisDto>.Failure(habitResult.Error);

            if (habitResult.Value!.Type != HabitType.PhysicalActivity)
                return Result<GoalAnalysisDto>.Failure(new Error(ErrorType.BadRequest, 
                    $"Тип привычки не {HabitType.PhysicalActivity.ToString()}"));
            
            var goal = $"{habitResult.Value!.Goal} - Вид активности " +
                       $"{habitResult.Value!.PhysicalActivityType.ToString()}" ;
            
            var analyzeResult = await aiService.AnalyzeGoalCompletionAsync(activityData.Value!, goal);
            
            return analyzeResult.IsSuccess
                ? Result<GoalAnalysisDto>.Success(analyzeResult.Value!)
                : Result<GoalAnalysisDto>.Failure(analyzeResult.Error);
        }
        catch (Exception)
        {
            return Result<GoalAnalysisDto>.Failure(new Error(ErrorType.ServerError,
                "Не удалось проанализировать данные об активности"));
        }
    }
}