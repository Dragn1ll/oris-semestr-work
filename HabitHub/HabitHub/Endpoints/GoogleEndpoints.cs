using System.Security.Claims;
using System.Text.Json;
using Application.Dto_s.Fit;
using Application.Interfaces.Services.MainServices;
using Application.Services.HelperServices.Options;
using AutoMapper;
using HabitHub.Requests.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HabitHub.Endpoints;

public static class GoogleEndpoints
{
    public static void MapGoogleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/google")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/token/add", AddGoogleToken);
        group.MapDelete("/token/remove", RemoveGoogleToken);
        group.MapGet("/token/contains", HasTokenAsync);
        group.MapPost("/fit/progress/analyze", GetUserFitProgressAsync);
    }
    
    private static async Task<IResult> AddGoogleToken(
    ClaimsPrincipal user,
    [FromBody] GoogleCodeExchangeRequest request,
    IGoogleService googleService,
    IOptions<GoogleOptions> options,
    ILogger<IGoogleService> logger)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            logger.LogWarning("Пустой код авторизации");
            return Results.BadRequest("Не указан код авторизации");
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "code", request.Code },
                { "client_id", options.Value.ClientId },
                { "client_secret", options.Value.ClientSecret },
                { "redirect_uri", options.Value.RedirectUri },
                { "grant_type", "authorization_code" }
            };

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token", 
                new FormUrlEncodedContent(parameters));

            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Google OAuth response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Google OAuth error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                return Results.Problem("Ошибка авторизации Google", 
                    statusCode: (int)response.StatusCode);
            }

            var responseData = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
            if (responseData == null || 
                string.IsNullOrEmpty(responseData.AccessToken) || 
                string.IsNullOrEmpty(responseData.RefreshToken))
            {
                logger.LogError("Неверный формат ответа Google");
                return Results.Problem("Неверный формат ответа от Google", statusCode: 500);
            }

            var userId = GetUserIdFromClaims(user);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var addResult = await googleService.AddGoogleToken(
                userId.Value, 
                responseData.AccessToken, 
                responseData.RefreshToken, 
                DateTime.UtcNow.AddSeconds(responseData.ExpiresIn));

            return addResult.IsSuccess
                ? Results.Ok()
                : Results.Problem(
                    addResult.Error!.Message, 
                    statusCode: (int)addResult.Error.ErrorType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении Google токена");
            return Results.Problem("Внутренняя ошибка сервера", statusCode: 500);
        }
    }

    private static async Task<IResult> RemoveGoogleToken(
        ClaimsPrincipal user,
        IGoogleService googleService
    )
    {
        var userId = GetUserIdFromClaims(user);
        
        var removeResult = await googleService.RemoveGoogleToken(userId!.Value);
        
        return removeResult.IsSuccess
            ? Results.Ok()
            : Results.Problem(removeResult.Error!.Message, statusCode: (int)removeResult.Error.ErrorType);
    }

    private static async Task<IResult> HasTokenAsync(
        ClaimsPrincipal user,
        IGoogleService googleService
    )
    {
        var userId = GetUserIdFromClaims(user);
        
        var hasResult = await googleService.HasTokenAsync(userId!.Value);
        
        return hasResult.IsSuccess
            ? Results.Ok(hasResult.Value)
            : Results.Problem(hasResult.Error!.Message, statusCode: (int)hasResult.Error.ErrorType);
    }

    private static async Task<IResult> GetUserFitProgressAsync(
        ClaimsPrincipal user,
        [FromBody] FitAnalyzeRequest request,
        IGoogleService googleService,
        IMapper mapper
    )
    {
        var userId = GetUserIdFromClaims(user);
        
        var getResult = await googleService.GetUserFitProgressAsync(userId!.Value, 
            mapper.Map<FitAnalyzeDto>(request));
        
        return getResult.IsSuccess
            ? Results.Ok(getResult.Value)
            : Results.Problem(getResult.Error!.Message, statusCode: (int)getResult.Error.ErrorType);
    }
    
    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var guid) ? guid : null;
    }
}