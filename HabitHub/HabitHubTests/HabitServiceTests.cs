using System.Linq.Expressions;
using Application.Dto_s;
using Application.Dto_s.Habit;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabitHubTests;

public class HabitServiceTests
{
    private readonly Mock<IHabitRepository> _habitRepoMock = new();
    private readonly Mock<IHabitProgressRepository> _progressRepoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<HabitService>> _loggerMock = new();
    private readonly HabitService _service;

    public HabitServiceTests()
    {
        _service = new HabitService(
            _habitRepoMock.Object,
            _progressRepoMock.Object,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    private Habit CreateTestHabit(Guid userId) => 
        Habit.Create(
            Guid.NewGuid(), 
            userId, 
            HabitType.PhysicalActivity, 
            PhysicalActivityType.Walking, 
            "10000 шагов", 
            true
        );

    private HabitProgress CreateTestProgress(Guid habitId) => 
        HabitProgress.Create(
            Guid.NewGuid(), 
            habitId, 
            DateOnly.FromDateTime(DateTime.Today), 
            75f
        );

    [Fact]
    public async Task AddAsync_Success_ReturnsIdDto()
    {
        var habitAddDto = new HabitAddDto();
        var habit = CreateTestHabit(Guid.NewGuid());
        var idDto = new IdDto { Id = habit.Id };
        
        _mapperMock.Setup(m => m.Map<Habit>(habitAddDto)).Returns(habit);
        _habitRepoMock.Setup(r => r.AddAsync(habit)).ReturnsAsync(Result.Success());
        _mapperMock.Setup(m => m.Map<IdDto>(habit)).Returns(idDto);

        var result = await _service.AddAsync(habitAddDto);

        Assert.True(result.IsSuccess);
        Assert.Equal(habit.Id, result.Value?.Id);
    }

    [Fact]
    public async Task AddAsync_RepositoryError_ReturnsFailure()
    {
        var habitAddDto = new HabitAddDto();
        var habit = CreateTestHabit(Guid.NewGuid());
        var error = new Error(ErrorType.ServerError, "DB error");
        
        _mapperMock.Setup(m => m.Map<Habit>(habitAddDto)).Returns(habit);
        _habitRepoMock.Setup(r => r.AddAsync(habit)).ReturnsAsync(Result.Failure(error));

        var result = await _service.AddAsync(habitAddDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task AddProgressAsync_NewProgress_Success()
    {
        var userId = Guid.NewGuid();
        var habit = CreateTestHabit(userId);
        var progressDto = new HabitProgressAddDto 
        { 
            HabitId = habit.Id, 
            Date = DateOnly.FromDateTime(DateTime.Today),
            PercentageCompletion = 50
        };

        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);
        
        _progressRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitProgressEntity, bool>>>()))
            .ReturnsAsync(Result<HabitProgress?>.Success(null)!);
        
        _progressRepoMock.Setup(r => r.AddAsync(It.IsAny<HabitProgress>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.AddProgressAsync(userId, progressDto);

        Assert.True(result.IsSuccess);
        _progressRepoMock.Verify(r => r.AddAsync(It.IsAny<HabitProgress>()), Times.Once);
        _progressRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Action<HabitProgressEntity>>()), Times.Never);
    }

    [Fact]
    public async Task AddProgressAsync_UpdateExisting_Success()
    {
        var userId = Guid.NewGuid();
        var habit = CreateTestHabit(userId);
        var existingProgress = CreateTestProgress(habit.Id);
        var progressDto = new HabitProgressAddDto 
        { 
            HabitId = habit.Id, 
            Date = existingProgress.Date,
            PercentageCompletion = 100
        };

        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);
        
        _progressRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitProgressEntity, bool>>>()))
            .ReturnsAsync(Result<HabitProgress?>.Success(existingProgress)!);
        
        _progressRepoMock.Setup(r => r.UpdateAsync(existingProgress.Id, It.IsAny<Action<HabitProgressEntity>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.AddProgressAsync(userId, progressDto);

        Assert.True(result.IsSuccess);
        _progressRepoMock.Verify(r => r.UpdateAsync(existingProgress.Id, It.IsAny<Action<HabitProgressEntity>>()), Times.Once);
    }

    [Fact]
    public async Task AddProgressAsync_NoPermission_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var habit = CreateTestHabit(otherUserId);
        var progressDto = new HabitProgressAddDto { HabitId = habit.Id };

        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(null)!);

        var result = await _service.AddProgressAsync(userId, progressDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_Success_ReturnsHabit()
    {
        var habit = CreateTestHabit(Guid.NewGuid());
        var habitDto = new HabitInfoDto { Id = habit.Id };
        
        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);
        
        _mapperMock.Setup(m => m.Map<HabitInfoDto>(habit)).Returns(habitDto);

        var result = await _service.GetByIdAsync(habit.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(habit.Id, result.Value?.Id);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_Success_ReturnsHabits()
    {
        var userId = Guid.NewGuid();
        var habits = new List<Habit> { CreateTestHabit(userId) };
        var habitDtos = habits.Select(h => new HabitInfoDto { Id = h.Id }).ToList();
        
        _habitRepoMock.Setup(r => r.GetAllByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<Habit>>.Success(habits));
        
        _mapperMock.Setup(m => m.Map<IEnumerable<HabitInfoDto>>(habits))
            .Returns(habitDtos);

        var result = await _service.GetAllByUserIdAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(habits[0].Id, result.Value?.First().Id);
    }

    [Fact]
    public async Task GetAllProgressByHabitIdAsync_Success_ReturnsProgress()
    {
        var habitId = Guid.NewGuid();
        var progresses = new List<HabitProgress> { CreateTestProgress(habitId) };
        var progressDtos = progresses.Select(p => new HabitProgressInfoDto { Id = p.Id }).ToList();
        
        _progressRepoMock.Setup(r => r.GetAllByFilterAsync(It.IsAny<Expression<Func<HabitProgressEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<HabitProgress>>.Success(progresses));
        
        _mapperMock.Setup(m => m.Map<IEnumerable<HabitProgressInfoDto>>(progresses))
            .Returns(progressDtos);

        var result = await _service.GetAllProgressByHabitIdAsync(habitId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(progresses[0].Id, result.Value?.First().Id);
    }

    [Fact]
    public async Task UpdateByIdAsync_Success_NoContent()
    {
        var userId = Guid.NewGuid();
        var habit = CreateTestHabit(userId);
        var habitDto = new HabitInfoDto { Id = habit.Id };
        
        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);
        
        _habitRepoMock.Setup(r => r.UpdateAsync(habitDto.Id, It.IsAny<Action<HabitEntity>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.UpdateByIdAsync(userId, habitDto);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateByIdAsync_NoPermission_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var habit = CreateTestHabit(otherUserId);
        var habitDto = new HabitInfoDto { Id = habit.Id };
        
        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(null)!);

        var result = await _service.UpdateByIdAsync(userId, habitDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task DeleteByIdAsync_Success_NoContent()
    {
        var userId = Guid.NewGuid();
        var habit = CreateTestHabit(userId);
        
        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(habit)!);
        
        _habitRepoMock.Setup(r => r.DeleteAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.DeleteByIdAsync(userId, habit.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteByIdAsync_NotFound_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        
        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ReturnsAsync(Result<Habit?>.Success(null)!);

        var result = await _service.DeleteByIdAsync(userId, habitId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task AddProgressAsync_Exception_ReturnsServerError()
    {
        var userId = Guid.NewGuid();
        var habit = CreateTestHabit(userId);
        var progressDto = new HabitProgressAddDto { HabitId = habit.Id };
        
        _habitRepoMock.Setup(r => r.GetByFilterAsync(It.IsAny<Expression<Func<HabitEntity, bool>>>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await _service.AddProgressAsync(userId, progressDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ServerError, result.Error?.ErrorType);
    }
}