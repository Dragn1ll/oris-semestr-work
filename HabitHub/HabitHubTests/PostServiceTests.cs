using System.Linq.Expressions;
using Application.Dto_s;
using Application.Dto_s.Post;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.HelperServices;
using Application.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;

namespace HabitHubTests;

public class PostServiceTests
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IMinioService> _minioMock = new();
    private readonly Mock<IPostRepository> _postRepoMock = new();
    private readonly Mock<IMediaFileRepository> _mediaRepoMock = new();
    private readonly Mock<ILikeRepository> _likeRepoMock = new();
    private readonly Mock<ICommentRepository> _commentRepoMock = new();
    private readonly PostService _service;

    public PostServiceTests()
    {
        _service = new PostService(
            _mapperMock.Object,
            _minioMock.Object,
            _postRepoMock.Object,
            _mediaRepoMock.Object,
            _likeRepoMock.Object,
            _commentRepoMock.Object
        );
    }

    private Post CreateTestPost() => Post.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Test post",
        DateTime.UtcNow
    );

    private MediaFile CreateTestMedia(Guid postId) => MediaFile.Create(
        Guid.NewGuid(),
        postId,
        ".jpg",
        MediaFileType.Image
    );

    [Fact]
    public async Task AddAsync_Success_ReturnsIdDto()
    {
        var postAddDto = new PostAddDto
        {
            MediaFiles = new List<IFormFile> { CreateFormFile("test.jpg", "image/jpeg") }
        };
        var post = CreateTestPost();
        var idDto = new IdDto { Id = post.Id };
    
        _mapperMock.Setup(m => m.Map<Post>(postAddDto)).Returns(post);
        _postRepoMock.Setup(r => r.AddAsync(post)).ReturnsAsync(Result.Success());
        _mediaRepoMock.Setup(r => r.AddAsync(
                It.IsAny<MediaFile>()))
            .ReturnsAsync(Result.Success());
        _minioMock.Setup(m => m.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), 
                It.IsAny<string>()))
            .ReturnsAsync(Result.Success());
    
        _mapperMock.Setup(m => m.Map<IdDto>(post)).Returns(idDto);

        var result = await _service.AddAsync(postAddDto);

        Assert.True(result.IsSuccess);
        Assert.Equal(post.Id, result.Value?.Id);
    }

    [Fact]
    public async Task AddAsync_UnsupportedFileType_ReturnsError()
    {
        var postAddDto = new PostAddDto
        {
            MediaFiles = new List<IFormFile> { CreateFormFile("test.unsupported", 
                "application/octet-stream") }
        };
        var post = CreateTestPost();
        
        _mapperMock.Setup(m => m.Map<Post>(postAddDto)).Returns(post);
        _postRepoMock.Setup(r => r.AddAsync(post)).ReturnsAsync(Result.Success());

        var result = await _service.AddAsync(postAddDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error?.ErrorType);
    }

    [Fact]
    public async Task AddLikeAsync_Success_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _likeRepoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<LikeEntity, bool>>>()))
            .ReturnsAsync(Result<Like?>.Success(null)!);
        _likeRepoMock.Setup(r => r.AddAsync(
            It.IsAny<Like>())).ReturnsAsync(Result.Success());

        var result = await _service.AddLikeAsync(userId, postId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddCommentAsync_Success_ReturnsIdDto()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var comment = "Test comment";
        
        _commentRepoMock.Setup(r => r.AddAsync(
            It.IsAny<Comment>())).ReturnsAsync(Result.Success());

        var result = await _service.AddCommentAsync(userId, postId, comment);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Success_ReturnsPostInfo()
    {
        var userId = Guid.NewGuid();
        var post = CreateTestPost();
        var postDto = new PostInfoDto { Id = post.Id };
        var media = CreateTestMedia(post.Id);
        var mediaUrl = "http://example.com/image.jpg";
    
        _postRepoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<PostEntity, bool>>>()))
            .ReturnsAsync(Result<Post?>.Success(post)!);
    
        _mapperMock.Setup(m => m.Map<PostInfoDto>(post)).Returns(postDto);
    
        _mediaRepoMock.Setup(r => r.GetAllByFilterAsync(
                It.IsAny<Expression<Func<MediaFileEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<MediaFile>>.Success([media]));
    
        _minioMock.Setup(m => m.GetFileUrlAsync($"{media.Id}{media.Extension}", 
                It.IsAny<int>()))
            .ReturnsAsync(Result<string>.Success(mediaUrl));
    
        _likeRepoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<LikeEntity, bool>>>()))
            .ReturnsAsync(Result<Like?>.Success(null)!);
    
        _likeRepoMock.Setup(r => r.GetAllByFilterAsync(
                It.IsAny<Expression<Func<LikeEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<Like>>.Success([]));
        _commentRepoMock.Setup(r => r.GetAllByFilterAsync(
                It.IsAny<Expression<Func<CommentEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<Comment>>.Success([]));

        var result = await _service.GetByIdAsync(userId, post.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value?.MediaFilesUrl!);
        Assert.Equal(mediaUrl, result.Value?.MediaFilesUrl.First());
    }

    [Fact]
    public async Task UpdateByIdAsync_NoAccess_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var postDto = new PostInfoDto { Id = postId };
        
        _postRepoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<PostEntity, bool>>>()))
            .ReturnsAsync(Result<Post?>.Success(null)!);

        var result = await _service.UpdateByIdAsync(userId, postId, postDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task DeleteByIdAsync_Success_DeletesMediaFiles()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var media = CreateTestMedia(postId);
        
        _postRepoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<PostEntity, bool>>>()))
            .ReturnsAsync(Result<Post?>.Success(CreateTestPost())!);
        _mediaRepoMock.Setup(r => r.GetAllByFilterAsync(
                It.IsAny<Expression<Func<MediaFileEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<MediaFile>>.Success([media]));
        _minioMock.Setup(m => m.DeleteFileAsync(media.Id.ToString()))
            .ReturnsAsync(Result.Success());
        _mediaRepoMock.Setup(r => r.DeleteAsync(
                It.IsAny<Expression<Func<MediaFileEntity, bool>>>()))
            .ReturnsAsync(Result.Success());
        _postRepoMock.Setup(r => r.DeleteAsync(
                It.IsAny<Expression<Func<PostEntity, bool>>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.DeleteByIdAsync(userId, postId);

        Assert.True(result.IsSuccess);
        _minioMock.Verify(m => m.DeleteFileAsync(media.Id.ToString()), Times.Once);
    }

    [Fact]
    public async Task DeleteLikeByPostIdAsync_NoAccess_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _likeRepoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<LikeEntity, bool>>>()))
            .ReturnsAsync(Result<Like?>.Success(null)!);

        var result = await _service.DeleteLikeByPostIdAsync(userId, postId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write("Test content");
        writer.Flush();
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}