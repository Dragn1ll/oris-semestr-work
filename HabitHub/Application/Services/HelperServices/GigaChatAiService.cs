using System.Text;
using System.Text.Json;
using Application.Dto_s.Fit;
using Application.Enums;
using Application.Interfaces.Services;
using Application.Interfaces.Services.HelperServices;
using Application.Utils;
using Domain.Models;
using GigaChatAdapter;
using Microsoft.Extensions.Configuration;

namespace Application.Services.HelperServices;

public class GigaChatAiService(IGigaChatApiClient gigaChatClient) : IAiService
{
    public async Task<Result<GoalAnalysisDto>> AnalyzeGoalCompletionAsync(
        ICollection<ActivityData> activities,
        string userGoal)
    {
        if (string.IsNullOrWhiteSpace(userGoal))
        {
            return Result<GoalAnalysisDto>.Failure(
                new Error(ErrorType.ServerError, "Цель не может быть пустой"));
        }

        if (activities == null! || activities.Count == 0)
        {
            return Result<GoalAnalysisDto>.Failure(
                new Error(ErrorType.ServerError, "Нет данных активности для анализа"));
        }

        try
        {
            var activitySummary = PrepareActivitySummary(activities);

            var prompt = $$"""
                **Цель:** "{{userGoal}}"

                **Сегодняшняя активность:**
                {{activitySummary}}

                **Требования к ответу:**
                1. Рассчитай процент выполнения цели (0-100%)
                2. Сравни фактические показатели с целью
                3. Укажи основные достижения
                4. Дай рекомендации по улучшению

                **Формат ответа (строгий JSON):**
                {
                    "completionPercentage": number,
                    "analysisSummary": string
                }
                """;

            var accessToken = await gigaChatClient.GetAccessTokenAsync();
            if (!accessToken.IsSuccess)
                return Result<GoalAnalysisDto>.Failure(accessToken.Error);
            
            var responseText = await gigaChatClient.SendMessageAsync(accessToken.Value!, prompt);
            if (!responseText.IsSuccess)
                return Result<GoalAnalysisDto>.Failure(responseText.Error);

            Console.WriteLine(responseText.Value);
            
            var result = ParseAiResponse(responseText.Value!);
            return Result<GoalAnalysisDto>.Success(result);
        }
        catch (JsonException)
        {
            return Result<GoalAnalysisDto>.Failure(
                new Error(ErrorType.ServerError, "Неверный формат ответа от AI"));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return Result<GoalAnalysisDto>.Failure(
                new Error(ErrorType.ServerError, "Ошибка анализа целей"));
        }
    }

    private string PrepareActivitySummary(ICollection<ActivityData> activities)
    {
        var summary = new StringBuilder();
        var totalSteps = activities.Sum(a => a.Steps);
        var totalCalories = activities.Sum(a => a.Calories);
        var totalDistance = activities.Sum(a => a.Distance) / 1000;
        var totalMinutes = activities.Sum(a => (a.EndTime - a.StartTime).Minutes);
        var mainActivity = activities.MaxBy(a => (a.EndTime - a.StartTime).Minutes);

        summary.AppendLine($"- Общее количество шагов: {totalSteps}");
        summary.AppendLine($"- Сожжено калорий: {totalCalories} ккал");
        summary.AppendLine($"- Пройдено дистанции: {totalDistance:F2} км");
        summary.AppendLine($"- Общее время активности: {totalMinutes} мин");

        if (mainActivity != null)
        {
            summary.AppendLine($"- Основная активность: {mainActivity.ActivityType} " +
                               $"({(mainActivity.EndTime - mainActivity.StartTime).Minutes} мин)");
        }

        return summary.ToString();
    }

    private GoalAnalysisDto ParseAiResponse(string aiResponse)
    {
        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}') + 1;
            var cleanJson = (jsonStart >= 0 && jsonEnd > jsonStart) 
                ? aiResponse[jsonStart..jsonEnd] 
                : aiResponse;
            
            using var document = JsonDocument.Parse(cleanJson);
            var root = document.RootElement;

            var result = new GoalAnalysisDto
            {
                CompletionPercentage = root.GetProperty("completionPercentage").GetDouble(),
                AnalysisSummary = root.GetProperty("analysisSummary").GetString() ?? string.Empty
            };

            return result;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Ошибка десериализации: {ex.Message}");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            throw new JsonException($"Неверный формат JSON: {ex.Message}");
        }
    }
}