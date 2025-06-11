using Application.Dto_s;
using Application.Dto_s.Post;
using Application.Enums;
using Application.Extensions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.HelperServices;
using Application.Interfaces.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Models;

namespace Application.Services.MainServices;

public class PostService(
    IMapper mapper,
    IMinioService minioService,
    IPostRepository postRepository,
    IMediaFileRepository mediaFileRepository,
    ILikeRepository likeRepository,
    ICommentRepository commentRepository
) : IPostService
{
    public async Task<Result<IdDto>> AddAsync(PostAddDto postAddDto)
    {
        try
        {
            var postModel = mapper.Map<Post>(postAddDto);
            
            var addPostResult = await postRepository.AddAsync(postModel);
            
            if (!addPostResult.IsSuccess)
                return Result<IdDto>.Failure(addPostResult.Error);

            foreach (var file in postAddDto.MediaFiles)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!MediaFileExtensions.TryGetMediaType(extension, out var mediaType))
                {
                    return Result<IdDto>.Failure(new Error(ErrorType.NotFound,
                        $"Тип медиафайла не поддерживается: {extension}"));
                }

                var mediaModel = MediaFile.Create(Guid.NewGuid(), postModel.Id, extension, mediaType);
                var addMediaResult = await mediaFileRepository.AddAsync(mediaModel);

                if (!addMediaResult.IsSuccess)
                    return Result<IdDto>.Failure(addMediaResult.Error);

                var minioAddResult = await minioService.UploadFileAsync(
                    $"{mediaModel.Id}{extension}",
                    file.OpenReadStream(),
                    file.ContentType);

                if (minioAddResult.IsSuccess) continue;
                await mediaFileRepository.DeleteAsync(m => m.Id == mediaModel.Id);
                return Result<IdDto>.Failure(minioAddResult.Error);
            }

            return Result<IdDto>.Success(mapper.Map<IdDto>(postModel));
        }
        catch (Exception ex)
        {
            return Result<IdDto>.Failure(new Error(ErrorType.ServerError,
                $"Ошибка при добавлении поста: {ex.Message}"));
        }
    }

    public async Task<Result> AddLikeAsync(Guid userId, Guid postId)
    {
        try
        {
            var getResult = await likeRepository.GetByFilterAsync(p => 
                p.UserId == userId && p.PostId == postId);
            if (getResult is { IsSuccess: true, Value: not null })
                return Result.Failure(new Error(ErrorType.BadRequest, 
                    "Лайк уже был поставлен"));
            
            var likeModel = Like.Create(Guid.NewGuid(), userId, postId);
            var result = await likeRepository.AddAsync(likeModel);

            return result.IsSuccess 
                ? Result.Success() 
                : Result.Failure(result.Error);
        }
        catch
        {
            return Result.Failure(new Error(ErrorType.ServerError, "Ошибка при добавлении лайка"));
        }
    }

    public async Task<Result<IdDto>> AddCommentAsync(Guid userId, Guid postId, string comment)
    {
        try
        {
            var commentModel = Comment.Create(Guid.NewGuid(), userId, postId, comment, DateTime.UtcNow);
            var result = await commentRepository.AddAsync(commentModel);

            return result.IsSuccess 
                ? Result<IdDto>.Success(new IdDto
                {
                    Id = commentModel.Id
                }) 
                : Result<IdDto>.Failure(result.Error);
        }
        catch
        {
            return Result<IdDto>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка при добавлении комментария"));
        }
    }

    public async Task<Result<IEnumerable<PostInfoDto>>> GetAllByNewAsync(Guid userId)
    {
        try
        {
            var getResult = await postRepository.GetAllByNewAsync();
            
            var postInfoDtos = mapper.Map<IEnumerable<PostInfoDto>>(getResult.Value).ToList();
            
            foreach (var postInfoDto in postInfoDtos)
            {
                await GetPostInfo(postInfoDto);
                postInfoDto.DidUserLiked = await DidUserLikedPost(userId, postInfoDto.Id);
            }

            return getResult.IsSuccess
                ? Result<IEnumerable<PostInfoDto>>.Success(postInfoDtos)
                : Result<IEnumerable<PostInfoDto>>.Failure(getResult.Error);
        }
        catch (Exception)
        {
            return Result<IEnumerable<PostInfoDto>>.Failure(new Error(ErrorType.ServerError,
                "Не удалось получить посты"));
        }
    }
    
    public async Task<Result<PostInfoDto>> GetByIdAsync(Guid userId, Guid postId)
    {
        try
        {
            var result = await postRepository.GetByFilterAsync(p => p.Id == postId);
            
            var postInfoDto = mapper.Map<PostInfoDto>(result.Value);
            
            await GetPostInfo(postInfoDto);
            postInfoDto.DidUserLiked = await DidUserLikedPost(userId, postId);

            return result.IsSuccess
                ? Result<PostInfoDto>.Success(postInfoDto)
                : Result<PostInfoDto>.Failure(result.Error);
        }
        catch
        {
            return Result<PostInfoDto>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка при получении поста"));
        }
    }

    public async Task<Result<IEnumerable<PostInfoDto>>> GetAllByUserIdAsync(Guid userId, Guid askedUserId)
    {
        try
        {
            var result = await postRepository.GetAllByFilterAsync(p => p.UserId == askedUserId);

            var postInfoDtos = mapper.Map<IEnumerable<PostInfoDto>>(result.Value).ToList();

            foreach (var postInfoDto in postInfoDtos)
            {
                await GetPostInfo(postInfoDto);
                postInfoDto.DidUserLiked = await DidUserLikedPost(userId, postInfoDto.Id);
            }
            
            return result.IsSuccess
                ? Result<IEnumerable<PostInfoDto>>.Success(postInfoDtos.OrderByDescending(p => p.DateTime))
                : Result<IEnumerable<PostInfoDto>>.Failure(result.Error);
        }
        catch
        {
            return Result<IEnumerable<PostInfoDto>>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка при получении постов"));
        }
    }

    public async Task<Result<IEnumerable<CommentInfoDto>>> GetAllCommentsByPostIdAsync(Guid postId)
    {
        try
        {
            var result = await commentRepository.GetAllByFilterAsync(c => c.PostId == postId);

            return result.IsSuccess
                ? Result<IEnumerable<CommentInfoDto>>.Success(mapper.Map<IEnumerable<CommentInfoDto>>(result.Value))
                : Result<IEnumerable<CommentInfoDto>>.Failure(result.Error);
        }
        catch
        {
            return Result<IEnumerable<CommentInfoDto>>.Failure(new Error(ErrorType.ServerError, 
                "Ошибка при получении комментариев"));
        }
    }

    public async Task<Result> UpdateByIdAsync(Guid userId, Guid postId, PostInfoDto postInfoDto)
    {
        try
        {
            if (!await HasPostAccess(userId, postId))
                return Result.Failure(new Error(ErrorType.BadRequest, 
                    "Нет прав на редактирование поста"));

            var result = await postRepository.UpdateAsync(postId, p => p.Text = postInfoDto.Text);

            return result.IsSuccess 
                ? Result.Success() 
                : Result.Failure(result.Error);
        }
        catch
        {
            return Result.Failure(new Error(ErrorType.ServerError, "Ошибка при обновлении поста"));
        }
    }

    public async Task<Result> DeleteByIdAsync(Guid userId, Guid postId)
    {
        try
        {
            if (!await HasPostAccess(userId, postId))
                return Result.Failure(new Error(ErrorType.BadRequest, 
                    "Нет прав на удаление поста"));

            var mediaFiles = await mediaFileRepository
                .GetAllByFilterAsync(p => p.PostId == postId);

            if (mediaFiles.IsSuccess)
            {
                foreach (var mediaFile in mediaFiles.Value!)
                {
                    await minioService.DeleteFileAsync(mediaFile.Id.ToString());
                    
                    await mediaFileRepository.DeleteAsync(m => m.Id == mediaFile.Id);
                }
            }
            
            var result = await postRepository.DeleteAsync(p => p.Id == postId);

            return result.IsSuccess 
                ? Result.Success() 
                : Result.Failure(result.Error);
        }
        catch
        {
            return Result.Failure(new Error(ErrorType.ServerError, "Ошибка при удалении поста"));
        }
    }

    public async Task<Result> DeleteLikeByPostIdAsync(Guid userId, Guid postId)
    {
        try
        {
            if (!await HasLikeAccess(userId, postId))
                return Result.Failure(new Error(ErrorType.BadRequest, "Нет прав на удаление лайка"));

            var result = await likeRepository.DeleteAsync(l => l.PostId == postId && l.UserId == userId);

            return result.IsSuccess 
                ? Result.Success() 
                : Result.Failure(result.Error);
        }
        catch
        {
            return Result.Failure(new Error(ErrorType.ServerError, "Ошибка при удалении лайка"));
        }
    }

    public async Task<Result> DeleteCommentByIdAsync(Guid userId, Guid commentId)
    {
        try
        {
            if (!await HasCommentAccess(userId, commentId))
                return Result.Failure(new Error(ErrorType.BadRequest, 
                    "Нет прав на удаление комментария"));

            var result = await commentRepository.DeleteAsync(p => p.Id == commentId);

            return result.IsSuccess 
                ? Result.Success() 
                : Result.Failure(result.Error);
        }
        catch
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                "Ошибка при удалении комментария"));
        }
    }

    private async Task<bool> HasPostAccess(Guid userId, Guid postId) =>
        await postRepository.GetByFilterAsync(p => p.Id == postId && p.UserId == userId)
        is {IsSuccess : true , Value : not null};

    private async Task<bool> HasLikeAccess(Guid userId, Guid postId) =>
        await likeRepository.GetByFilterAsync(l => l.PostId == postId && l.UserId == userId)
        is {IsSuccess : true , Value : not null};

    private async Task<bool> HasCommentAccess(Guid userId, Guid commentId) =>
        await commentRepository.GetByFilterAsync(c => c.Id == commentId 
                                                       && c.UserId == userId) 
        is {IsSuccess : true , Value : not null};

    private async Task GetPostInfo(PostInfoDto postInfoDto)
    {
        var mediafilesResult = await mediaFileRepository
            .GetAllByFilterAsync(p => p.PostId == postInfoDto.Id);

        var enumerable = mediafilesResult.Value!
            .Select(m => minioService.GetFileUrlAsync($"{m.Id}{m.Extension}").Result.Value);
                
        postInfoDto.MediaFilesUrl = enumerable!;
                
        postInfoDto.LikesCount = (uint)(await likeRepository
                .GetAllByFilterAsync(p => p.PostId == postInfoDto.Id))
            .Value!
            .Count();
                
        postInfoDto.CommentsCount = (uint)(await commentRepository
                .GetAllByFilterAsync(p => p.PostId == postInfoDto.Id))
            .Value!
            .Count();
    }
    
    private async Task<bool> DidUserLikedPost(Guid userId, Guid postId) => await likeRepository
        .GetByFilterAsync(l => l.PostId == postId && l.UserId == userId) 
        is {IsSuccess: true, Value: not null};
}