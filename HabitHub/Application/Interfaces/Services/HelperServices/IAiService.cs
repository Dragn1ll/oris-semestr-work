using Application.Dto_s.Fit;
using Application.Utils;
using Domain.Models;

namespace Application.Interfaces.Services.HelperServices;

public interface IAiService
{
    Task<Result<GoalAnalysisDto>> AnalyzeGoalCompletionAsync(ICollection<ActivityData> activities, 
        string userGoal);
}