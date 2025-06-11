using Application.Dto_s;
using Application.Dto_s.Post;
using Application.Utils;

namespace Application.Interfaces.Services.MainServices;

public interface IPostService
{
    Task<Result<IdDto>> AddAsync(PostAddDto postAddDto);
    Task<Result> AddLikeAsync(Guid userId, Guid postId);
    Task<Result<IdDto>> AddCommentAsync(Guid userId, Guid postId, string comment);
    Task<Result<IEnumerable<PostInfoDto>>> GetAllByNewAsync(Guid userId);
    Task<Result<PostInfoDto>> GetByIdAsync(Guid userId, Guid postId);
    Task<Result<IEnumerable<PostInfoDto>>> GetAllByUserIdAsync(Guid userId, Guid askedUserId);
    Task<Result<IEnumerable<CommentInfoDto>>> GetAllCommentsByPostIdAsync(Guid postId);
    Task<Result> UpdateByIdAsync(Guid userId, Guid postId, PostInfoDto postInfoDto);
    Task<Result> DeleteByIdAsync(Guid userId, Guid postId);
    Task<Result> DeleteLikeByPostIdAsync(Guid userId, Guid postId);
    Task<Result> DeleteCommentByIdAsync(Guid userId, Guid commentId);
}