using Application.Dto_s.Fit;
using Application.Utils;

namespace Application.Interfaces.Services.MainServices;

public interface IGoogleService
{
    Task<Result> AddGoogleToken(Guid userId, string accessToken, string? refreshToken, DateTime expiresAt);
    Task<Result> RemoveGoogleToken(Guid userId);
    Task<Result<bool>> HasTokenAsync(Guid userId);
    Task<Result<GoalAnalysisDto>> GetUserFitProgressAsync(Guid userId, FitAnalyzeDto analyzeDto);
}