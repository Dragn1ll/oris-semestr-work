using Application.Dto_s.Message;
using Application.Dto_s.User;
using Application.Interfaces.Services.MainServices;
using Microsoft.AspNetCore.Mvc;

namespace HabitHub.Endpoints;

using System.Security.Claims;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapGet("/get/{userId:guid}", GetUserByIdAsync);
        group.MapPut("/put", UpdateByIdAsync);
        group.MapDelete("/delete", DeleteByIdAsync);
        group.MapPost("/send/message", SendMessage);
    }

    private static async Task<IResult> GetUserByIdAsync(Guid userId, IUserService userService)
    {
        if (userId == Guid.Empty)
            return Results.BadRequest("Некорректный Id");

        var getResult = await userService.GetByIdAsync(userId);
        
        return getResult is { IsSuccess: true, Value: not null }
            ? Results.Ok(getResult.Value)
            : Results.Problem(getResult.Error!.Message, statusCode: (int)getResult.Error.ErrorType);
    }
    
    private static async Task<IResult> UpdateByIdAsync(
        ClaimsPrincipal user,
        [FromBody] UserInfoDto userInfoDto,
        IUserService userService)
    {
        var userId = GetUserIdFromClaims(user);

        var result = await userService.UpdateByIdAsync(userId!.Value, userInfoDto);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    private static async Task<IResult> DeleteByIdAsync(
        ClaimsPrincipal user,
        IUserService userService,
        HttpContext context)
    {
        var userId = GetUserIdFromClaims(user);

        var result = await userService.DeleteByIdAsync(userId!.Value);

        if (!result.IsSuccess) 
            return Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
        context.Response.Cookies.Append("jwt-cookies", "", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        });

        return Results.Ok();
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

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var guid) ? guid : null;
    }
}