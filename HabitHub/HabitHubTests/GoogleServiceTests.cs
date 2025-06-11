using System.Linq.Expressions;
using Application.Dto_s.Fit;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.HelperServices;
using Application.Services.MainServices;
using Application.Utils;
using Domain.Entities;
using Domain.Enums;
using Domain.Models;
using Moq;

namespace HabitHubTests;

public class GoogleServiceTests
{
    private readonly Mock<IGoogleTokenStore> _tokenStoreMock = new();
    private readonly Mock<IGoogleFitService> _fitServiceMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();
    private readonly Mock<IHabitRepository> _habitRepositoryMock = new();
    private readonly GoogleService _googleService;

    public GoogleServiceTests()
    {
        _googleService = new GoogleService(
            _tokenStoreMock.Object,
            _fitServiceMock.Object,
            _aiServiceMock.Object,
            _habitRepositoryMock.Object
        );
    }

    [Fact]
    public async Task AddGoogleToken_Success_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _tokenStoreMock.Setup(x => x.StoreTokenAsync(userId, "token", 
                "refresh", It.IsAny<DateTime>()))
            .ReturnsAsync(Result.Success());

        var result = await _googleService.AddGoogleToken(userId, "token", 
            "refresh", DateTime.UtcNow.AddHours(1));
        
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddGoogleToken_Failure_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var error = new Error(ErrorType.BadRequest, "Error");
        _tokenStoreMock.Setup(x => x.StoreTokenAsync(userId, "token",
                "refresh", It.IsAny<DateTime>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await _googleService.AddGoogleToken(userId, "token", 
            "refresh", DateTime.UtcNow.AddHours(1));
        
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task AddGoogleToken_Exception_ReturnsServerError()
    {
        var userId = Guid.NewGuid();
        _tokenStoreMock.Setup(x => x.StoreTokenAsync(userId, "token", 
                "refresh", It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception());

        var result = await _googleService.AddGoogleToken(userId, "token", 
            "refresh", DateTime.UtcNow.AddHours(1));

        
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ServerError, result.Error?.ErrorType);
    }

    [Fact]
    public async Task RemoveGoogleToken_Success_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _tokenStoreMock.Setup(x => x.RemoveTokenAsync(userId))
            .ReturnsAsync(Result.Success());

        var result = await _googleService.RemoveGoogleToken(userId);
        
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RemoveGoogleToken_Failure_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var error = new Error(ErrorType.NotFound, "Not found");
        _tokenStoreMock.Setup(x => x.RemoveTokenAsync(userId))
            .ReturnsAsync(Result.Failure(error));

        var result = await _googleService.RemoveGoogleToken(userId);

        
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task RemoveGoogleToken_Exception_ReturnsServerError()
    {
        var userId = Guid.NewGuid();
        _tokenStoreMock.Setup(x => x.RemoveTokenAsync(userId))
            .ThrowsAsync(new Exception());

        var result = await _googleService.RemoveGoogleToken(userId);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ServerError, result.Error?.ErrorType);
    }

    [Fact]
    public async Task HasTokenAsync_Success_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _tokenStoreMock.Setup(x => x.HasTokenAsync(userId))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _googleService.HasTokenAsync(userId);

        
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task HasTokenAsync_Failure_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var error = new Error(ErrorType.ServerError, "Error");
        _tokenStoreMock.Setup(x => x.HasTokenAsync(userId))
            .ReturnsAsync(Result<bool>.Failure(error));

        var result = await _googleService.HasTokenAsync(userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task HasTokenAsync_Exception_ReturnsServerError()
    {
        var userId = Guid.NewGuid();
        _tokenStoreMock.Setup(x => x.HasTokenAsync(userId))
            .ThrowsAsync(new Exception());

        var result = await _googleService.HasTokenAsync(userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ServerError, result.Error?.ErrorType);
    }

    [Fact]
    public async Task GetUserFitProgressAsync_Success_ReturnsAnalysis()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var analyzeDto = new FitAnalyzeDto
        {
            HabitId = habitId, 
            FromDate = DateTime.Now.AddDays(-1), 
            ToDate = DateTime.Now
        };
        var activities = new List<ActivityData>();
        var habit = Habit.Create(
            habitId, 
            userId, 
            HabitType.PhysicalActivity, 
            PhysicalActivityType.Walking, 
            "10000 шагов", 
            true
        );
        var analysisDto = new GoalAnalysisDto { CompletionPercentage = 75 };

        _fitServiceMock.Setup(x => x.GetActivityDataAsync(userId, analyzeDto.FromDate, 
                analyzeDto.ToDate))
            .ReturnsAsync(Result<ICollection<ActivityData>>.Success(activities));

        _habitRepositoryMock.Setup(x => x.GetByFilterAsync(
                It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);

        _aiServiceMock.Setup(x => x.AnalyzeGoalCompletionAsync(activities, 
                It.IsAny<string>()))
            .ReturnsAsync(Result<GoalAnalysisDto>.Success(analysisDto));

        var result = await _googleService.GetUserFitProgressAsync(userId, analyzeDto);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(75, result.Value?.CompletionPercentage);
    }

    [Fact]
    public async Task GetUserFitProgressAsync_FitServiceError_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var analyzeDto = new FitAnalyzeDto
        {
            HabitId = habitId,
            FromDate = DateTime.Now.AddDays(-1),
            ToDate = DateTime.Now
        };
        var error = new Error(ErrorType.ServerError, "Fit error");

        _fitServiceMock.Setup(x => x.GetActivityDataAsync(userId, analyzeDto.FromDate, 
                analyzeDto.ToDate))
            .ReturnsAsync(Result<ICollection<ActivityData>>.Failure(error));

        var result = await _googleService.GetUserFitProgressAsync(userId, analyzeDto);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetUserFitProgressAsync_HabitNotFound_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var analyzeDto = new FitAnalyzeDto
        {
            HabitId = habitId,
            FromDate = DateTime.Now.AddDays(-1),
            ToDate = DateTime.Now
        };
        var error = new Error(ErrorType.NotFound, "Not found");

        _fitServiceMock.Setup(x => x.GetActivityDataAsync(userId, analyzeDto.FromDate, 
                analyzeDto.ToDate))
            .ReturnsAsync(Result<ICollection<ActivityData>>.Success(new List<ActivityData>()));

        _habitRepositoryMock.Setup(x => x.GetByFilterAsync(
                It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Failure(error)!);

        var result = await _googleService.GetUserFitProgressAsync(userId, analyzeDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetUserFitProgressAsync_AiServiceError_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var analyzeDto = new FitAnalyzeDto
        {
            HabitId = habitId,
            FromDate = DateTime.Now.AddDays(-1),
            ToDate = DateTime.Now
        };
        var habit = Habit.Create(
            habitId, 
            userId, 
            HabitType.PhysicalActivity, 
            PhysicalActivityType.Running, 
            "5 км", 
            true
        );
        var error = new Error(ErrorType.ServerError, "AI error");

        _fitServiceMock.Setup(x => x.GetActivityDataAsync(userId, analyzeDto.FromDate, 
                analyzeDto.ToDate))
            .ReturnsAsync(Result<ICollection<ActivityData>>.Success(new List<ActivityData>()));

        _habitRepositoryMock.Setup(x => x.GetByFilterAsync(
                It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);

        _aiServiceMock.Setup(x => x.AnalyzeGoalCompletionAsync(
                It.IsAny<ICollection<ActivityData>>(), It.IsAny<string>()))
            .ReturnsAsync(Result<GoalAnalysisDto>.Failure(error));

        var result = await _googleService.GetUserFitProgressAsync(userId, analyzeDto);
        
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetUserFitProgressAsync_Exception_ReturnsServerError()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var analyzeDto = new FitAnalyzeDto
        {
            HabitId = habitId,
            FromDate = DateTime.Now.AddDays(-1),
            ToDate = DateTime.Now
        };

        _fitServiceMock.Setup(x => x.GetActivityDataAsync(userId, analyzeDto.FromDate, 
                analyzeDto.ToDate))
            .ThrowsAsync(new Exception());

        var result = await _googleService.GetUserFitProgressAsync(userId, analyzeDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ServerError, result.Error?.ErrorType);
    }
}
