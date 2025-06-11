using System.Security.Claims;
using Application.Dto_s;
using Application.Dto_s.User;
using Application.Interfaces.Auth;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.MainServices;
using AutoMapper;
using Domain.Models;
using HabitHub.Filters;
using HabitHub.Requests.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HabitHub.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        
        group.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();
        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();
        group.MapPost("/refresh", Refresh);
        group.MapPost("/logout", Logout);
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest registerRequest,
        IUserService userService,
        IMapper mapper,
        IPasswordHasher passwordHasher
    )
    {
        var userAddDto = mapper.Map<UserAddDto>(registerRequest);
        
        userAddDto.PasswordHash = passwordHasher.GetPasswordHash(registerRequest.Password);
        
        var addResult = await userService.AddAsync(userAddDto);
        
        return addResult.IsSuccess
            ? Results.Created()
            : Results.Problem(addResult.Error!.Message, statusCode: (int)addResult.Error.ErrorType);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest loginRequest,
        IUserService userService,
        IPasswordHasher passwordHasher,
        IJwtWorker jwtWorker,
        IRefreshTokenRepository refreshTokenRepository,
        HttpContext context
    )
    {
        var userAuthInfoDto = await userService.GetUserAuthInfoAsync(loginRequest.Email);
        if (!userAuthInfoDto.IsSuccess || userAuthInfoDto.Value == null)
            return Results.Problem("Пользователь с таким email не найден", statusCode: 404);

        var isValidate = passwordHasher.Validate(loginRequest.Password, userAuthInfoDto.Value.PasswordHash);
        if (!isValidate)
            return Results.Problem("Неверный пароль", statusCode: 400);

        var accessToken = jwtWorker.GenerateJwtToken(userAuthInfoDto.Value);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            userAuthInfoDto.Value.Id,
            jwtWorker.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(7),
            false
        );

        var checkToken = await refreshTokenRepository
            .GetByFilterAsync(rt => rt.UserId == userAuthInfoDto.Value.Id);

        if (!checkToken.IsSuccess || checkToken.Value == null)
            await refreshTokenRepository.AddAsync(refreshToken);
        else
            await refreshTokenRepository.UpdateAsync(checkToken.Value.Id, rt =>
            {
                rt.Token = refreshToken.Token;
                rt.Expires = refreshToken.Expires;
                rt.IsRevoked = refreshToken.IsRevoked;
            });
        
        context.Response.Cookies.Append("refresh-token", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.Expires
        });

        return Results.Ok(new
        {
            id = userAuthInfoDto.Value.Id,
            token = accessToken
        });
    }

    private static async Task<IResult> Refresh(
        HttpContext context,
        IJwtWorker jwtWorker,
        IRefreshTokenRepository refreshTokenRepository
    )
    {
        var refreshToken = context.Request.Cookies["refresh-token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.Problem("No refresh token", statusCode: 400);

        var getTokenResult = await refreshTokenRepository.GetByFilterAsync(rt =>
            rt.Token == refreshToken && !rt.IsRevoked && rt.Expires > DateTime.UtcNow);

        if (!getTokenResult.IsSuccess || getTokenResult.Value == null)
            return Results.Problem(statusCode: 401);

        var userId = getTokenResult.Value.UserId;

        var newAccessToken = jwtWorker.GenerateJwtToken(new UserAuthInfoDto { Id = userId });
        var newRefreshToken = jwtWorker.GenerateRefreshToken();

        await refreshTokenRepository.UpdateAsync(getTokenResult.Value.Id, rt =>
        {
            rt.Token = newRefreshToken;
            rt.Expires = DateTime.UtcNow.AddDays(7);
        });

        context.Response.Cookies.Append("refresh-token", newRefreshToken, new CookieOptions
        {
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Results.Ok(new
        {
            token = newAccessToken
        });
    }

    private static async Task<IResult> Logout(
        HttpContext context,
        IRefreshTokenRepository refreshTokenRepository
    )
    {
        var refreshToken = context.Request.Cookies["refresh-token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.Problem("No refresh token", statusCode: 400);
        
        var getTokenResult = await refreshTokenRepository.GetByFilterAsync(rt => 
            rt.Token == refreshToken && !rt.IsRevoked && rt.Expires > DateTime.UtcNow);
        if (!getTokenResult.IsSuccess || getTokenResult.Value == null)
            return Results.Problem("No refresh token", statusCode: 400);

        var putResult = await refreshTokenRepository.UpdateAsync(getTokenResult.Value.Id,
            rt => rt.IsRevoked = true);
        if (!putResult.IsSuccess)
            return Results.Problem(statusCode: 500);
        
        context.Response.Cookies.Delete("refresh-token");
        return Results.Ok();
    }
}