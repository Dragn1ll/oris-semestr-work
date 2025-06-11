using System.Linq.Expressions;
using Application.Dto_s;
using Application.Dto_s.User;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Entities;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabitHubTests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(
            _repoMock.Object,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    private User CreateTestUser() => new User(
        id: Guid.NewGuid(),
        name: "Test",
        surname: "User",
        patronymic: null,
        email: "test@example.com",
        passwordHash: "hashed_password",
        status: "Active",
        birthday: new DateOnly(1990, 1, 1)
    );

    [Fact]
    public async Task AddAsync_Success_ReturnsIdDto()
    {
        var userAddDto = new UserAddDto { Email = "new@example.com" };
        var user = CreateTestUser();
        var idDto = new IdDto { Id = user.Id };

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(null)!);
        _mapperMock.Setup(m => m.Map<User>(userAddDto)).Returns(user);
        _repoMock.Setup(r => r.AddAsync(user)).ReturnsAsync(Result.Success());
        _mapperMock.Setup(m => m.Map<IdDto>(user)).Returns(idDto);

        var result = await _service.AddAsync(userAddDto);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value?.Id);
    }

    [Fact]
    public async Task AddAsync_EmailExists_ReturnsError()
    {
        var userAddDto = new UserAddDto { Email = "existing@example.com" };
        var existingUser = CreateTestUser();

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(existingUser)!);

        var result = await _service.AddAsync(userAddDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_Success_ReturnsUser()
    {
        var user = CreateTestUser();
        var userDto = new UserInfoDto { Id = user.Id };

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(user)!);
        _mapperMock.Setup(m => m.Map<UserInfoDto>(user)).Returns(userDto);

        var result = await _service.GetByIdAsync(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsError()
    {
        var userId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(null)!);

        var result = await _service.GetByIdAsync(userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error?.ErrorType);
    }

    [Fact]
    public async Task GetUserAuthInfoAsync_Success_ReturnsAuthInfo()
    {
        var user = CreateTestUser();
        var authDto = new UserAuthInfoDto { Id = user.Id, PasswordHash = user.PasswordHash };

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(user)!);
        _mapperMock.Setup(m => m.Map<UserAuthInfoDto>(user)).Returns(authDto);

        var result = await _service.GetUserAuthInfoAsync(user.Email);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.PasswordHash, result.Value?.PasswordHash);
    }

    [Fact]
    public async Task UpdateByIdAsync_Success_ReturnsSuccess()
    {
        var user = CreateTestUser();
        var userDto = new UserInfoDto { Id = user.Id };

        _repoMock.Setup(r => r.UpdateAsync(user.Id, 
                It.IsAny<Action<UserEntity>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.UpdateByIdAsync(user.Id, userDto);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteByIdAsync_Success_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();

        _repoMock.Setup(r => r.DeleteAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.DeleteByIdAsync(userId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddAsync_RepositoryError_ReturnsFailure()
    {
        var userAddDto = new UserAddDto();
        var error = new Error(ErrorType.ServerError, "DB error");

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(null)!);
        _repoMock.Setup(r => r.AddAsync(
            It.IsAny<User>())).ReturnsAsync(Result.Failure(error));

        var result = await _service.AddAsync(userAddDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetUserAuthInfoAsync_NotFound_ReturnsError()
    {
        var email = "notfound@example.com";

        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<UserEntity, bool>>>()))
            .ReturnsAsync(Result<User?>.Success(null)!);

        var result = await _service.GetUserAuthInfoAsync(email);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error?.ErrorType);
    }

    [Fact]
    public async Task UpdateByIdAsync_RepositoryError_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var userDto = new UserInfoDto();
        var error = new Error(ErrorType.NotFound, "User not found");

        _repoMock.Setup(r => r.UpdateAsync(userId, 
                It.IsAny<Action<UserEntity>>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await _service.UpdateByIdAsync(userId, userDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}