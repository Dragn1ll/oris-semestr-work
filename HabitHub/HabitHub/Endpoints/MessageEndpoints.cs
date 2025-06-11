using System.Security.Claims;
using Application.Dto_s.Message;
using Application.Interfaces.Services.MainServices;
using Microsoft.AspNetCore.Mvc;

namespace HabitHub.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messages")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/send", SendMessage);
        group.MapGet("/companions", GetAllCompanionsAsync);
    }
    
    private static async Task<IResult> SendMessage(
        ClaimsPrincipal user,
        [FromBody] MessageAddDto messageAddDto,
        IMessageService messageService
    )
    {
        var userId = GetUserIdFromClaims(user);
        messageAddDto.SenderId = userId!.Value;
        
        var result = await messageService.AddAsync(messageAddDto);
        
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static async Task<IResult> GetAllCompanionsAsync(
        ClaimsPrincipal user,
        IMessageService messageService
    )
    {
        var userId = GetUserIdFromClaims(user);
        
        var result = await messageService.GetAllCompanionsIdByUserIdAsync(userId!.Value);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var guid) ? guid : null;
    }
}