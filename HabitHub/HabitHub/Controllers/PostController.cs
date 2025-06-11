using System.Security.Claims;
using Application.Dto_s.Post;
using Application.Interfaces.Services.MainServices;
using HabitHub.Requests.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitHub.Controllers;

[Authorize]
[Route("api/posts")]
public class PostController(IPostService postService) : ControllerBase
{
    [HttpPost("add")]
    public async Task<IResult> AddPostAsync(
        [FromForm] PostAddDto postAddDto
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        if(postAddDto.UserId != userId)
            return Results.Problem("Нельзя создать пост по id другого пользователя", statusCode:400);
        
        var result = await postService.AddAsync(postAddDto);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpPost("add/{postId:guid}/like")]
    public async Task<IResult> AddLikeAsync(
        Guid postId
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        var result = await postService.AddLikeAsync(userId, postId);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpPost("add/{postId:guid}/comment")]
    public async Task<IResult> AddCommentAsync(
        Guid postId,
        [FromBody] AddCommentRequest addCommentRequest
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;
        
        var result = await postService.AddCommentAsync(userId, postId, addCommentRequest.Comment);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }

    [HttpGet("get/all")]
    public async Task<IResult> GetAllPostsByNewAsync()
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        var result = await postService.GetAllByNewAsync(userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpGet("get/{postId:guid}")]
    public async Task<IResult> GetPostByIdAsync(
        Guid postId
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;
        
        var result = await postService.GetByIdAsync(userId, postId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpGet("get/user/{userId:guid}")]
    public async Task<IResult> GetAllPostsByUserIdAsync(
        Guid userId
        )
    {
        if (GetUserIdOrError(out var myUserId) is { } error)
            return error;
        
        var result = await postService.GetAllByUserIdAsync(myUserId, userId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpGet("get/{postId:guid}/comments")]
    public async Task<IResult> GetAllCommentsByPostIdAsync(
        Guid postId
        )
    {
        var result = await postService.GetAllCommentsByPostIdAsync(postId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpPut("put/{postId:guid}")]
    public async Task<IResult> UpdatePostAsync(
        Guid postId,
        [FromBody] PostInfoDto postInfoDto
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        var result = await postService.UpdateByIdAsync(userId, postId, postInfoDto);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpDelete("delete/{postId:guid}")]
    public async Task<IResult> DeletePostAsync(
        Guid postId
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        var result = await postService.DeleteByIdAsync(userId, postId);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpDelete("delete/{postId:guid}/like")]
    public async Task<IResult> DeleteLikeAsync(
        Guid postId
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        var result = await postService.DeleteLikeByPostIdAsync(userId, postId);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    [HttpDelete("delete/comments/{commentId:guid}")]
    public async Task<IResult> DeleteCommentAsync( 
        Guid commentId
        )
    {
        if (GetUserIdOrError(out var userId) is { } error)
            return error;

        var result = await postService.DeleteCommentByIdAsync(userId, commentId);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error!.Message, statusCode: (int)result.Error.ErrorType);
    }
    
    private IResult GetUserIdOrError(out Guid userId)
    {
        userId = GetUserIdFromClaims(User) ?? Guid.Empty;
        return userId == Guid.Empty 
            ? Results.Problem(statusCode:401) 
            : null!;
    }
    
    private Guid? GetUserIdFromClaims(ClaimsPrincipal userClaim)
    {
        var idStr = userClaim.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idStr, out var guid) ? guid : null;
    }
}