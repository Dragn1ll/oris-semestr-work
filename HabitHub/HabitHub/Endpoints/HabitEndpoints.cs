using System.Security.Claims;
using Application.Dto_s.Habit;
using Application.Interfaces.Services.MainServices;
using Microsoft.AspNetCore.Mvc;

namespace HabitHub.Endpoints;

public static class HabitEndpoints
{
    public static void MapHabitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/habits")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/add", AddHabitAsync);
        group.MapGet("/get/{habitId:guid}", GetHabitByIdAsync);
        group.MapGet("/get/all", GetAllHabitsByUserAsync);
        group.MapPut("/put", UpdateHabitAsync);
        group.MapDelete("/delete/{habitId:guid}", DeleteHabitAsync);
        
        group.MapPost("/progress/add", AddHabitProgressAsync);
        group.MapGet("/{habitId:guid}/progress/get/all", GetAllProgressByHabitIdAsync);
    }

    private static async Task<IResult> AddHabitAsync(
        ClaimsPrincipal user,
        [FromBody] HabitAddDto habitAddDto,
        IHabitService habitService)
    {
        var userId = GetUserIdFromClaims(user);

        habitAddDto.UserId = userId!.Value;

        var result = await habitService.AddAsync(habitAddDto);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> AddHabitProgressAsync(
        ClaimsPrincipal user,
        [FromBody] HabitProgressAddDto habitProgressAddDto,
        IHabitService habitService)
    {
        var userId = GetUserIdFromClaims(user);
        
        var result = await habitService.AddProgressAsync(userId!.Value, habitProgressAddDto);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> GetHabitByIdAsync(
        [FromRoute] Guid habitId,
        IHabitService habitService)
    {
        var result = await habitService.GetByIdAsync(habitId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> GetAllHabitsByUserAsync(
        ClaimsPrincipal user,
        IHabitService habitService)
    {
        var userId = GetUserIdFromClaims(user);

        var result = await habitService.GetAllByUserIdAsync(userId!.Value);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> GetAllProgressByHabitIdAsync(
        [FromRoute] Guid habitId,
        IHabitService habitService)
    {
        var result = await habitService.GetAllProgressByHabitIdAsync(habitId);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> UpdateHabitAsync(
        ClaimsPrincipal user,
        [FromBody] HabitInfoDto habitInfoDto,
        IHabitService habitService)
    {
        var userId = GetUserIdFromClaims(user);

        var result = await habitService.UpdateByIdAsync(userId!.Value, habitInfoDto);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> DeleteHabitAsync(
        ClaimsPrincipal user,
        [FromRoute] Guid habitId,
        IHabitService habitService)
    {
        var userId = GetUserIdFromClaims(user);

        var result = await habitService.DeleteByIdAsync(userId!.Value, habitId);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var guid) ? guid : null;
    }
}