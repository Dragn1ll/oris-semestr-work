using System.Linq.Expressions;
using Application.Dto_s;
using Application.Dto_s.Message;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Entities;
using Domain.Models;
using Moq;

namespace HabitHubTests;

public class MessageServiceTests
{
    private readonly Mock<IMessageRepository> _repoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly MessageService _service;

    public MessageServiceTests()
    {
        _service = new MessageService(
            _repoMock.Object,
            _mapperMock.Object
        );
    }

    private Message CreateTestMessage(Guid senderId, Guid recipientId) => 
        Message.Create(
            Guid.NewGuid(),
            recipientId,
            senderId,
            "Test message",
            DateTime.UtcNow
        );

    private MessageInfoDto CreateMessageInfoDto(Message message) => 
        new MessageInfoDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            RecipientId = message.RecipientId,
            Text = message.Text,
            DateTime = message.DateTime
        };

    [Fact]
    public async Task AddAsync_Success_ReturnsIdDto()
    {
        var messageAddDto = new MessageAddDto();
        var message = CreateTestMessage(Guid.NewGuid(), Guid.NewGuid());
        var idDto = new IdDto { Id = message.Id };
        
        _mapperMock.Setup(m => m.Map<Message>(messageAddDto)).Returns(message);
        _repoMock.Setup(r => r.AddAsync(message)).ReturnsAsync(Result.Success());
        _mapperMock.Setup(m => m.Map<IdDto>(message)).Returns(idDto);

        var result = await _service.AddAsync(messageAddDto);

        Assert.True(result.IsSuccess);
        Assert.Equal(message.Id, result.Value?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UserHasAccess_ReturnsMessage()
    {
        var userId = Guid.NewGuid();
        var message = CreateTestMessage(userId, Guid.NewGuid());
        var messageDto = CreateMessageInfoDto(message);
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(message)!);
        
        _mapperMock.Setup(m => m.Map<MessageInfoDto>(message)).Returns(messageDto);

        var result = await _service.GetByIdAsync(userId, message.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(message.Id, result.Value?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NoAccess_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = CreateTestMessage(otherUserId, otherUserId);
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(null)!);

        var result = await _service.GetByIdAsync(userId, message.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error?.ErrorType);
    }

    [Fact]
    public async Task GetAllByUsersIdAsync_Success_ReturnsMessages()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var messages = new List<Message>
        {
            CreateTestMessage(user1, user2),
            CreateTestMessage(user2, user1)
        };
        
        var messageDtos = messages.Select(CreateMessageInfoDto).ToList();
        
        _repoMock.Setup(r => r.GetAllByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<IEnumerable<Message>>.Success(messages));
        
        _mapperMock.Setup(m => m.Map<IEnumerable<MessageInfoDto>>(messages))
            .Returns(messageDtos);

        var result = await _service.GetAllByUsersIdAsync(user1, user2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.Count());
    }

    [Fact]
    public async Task GetAllCompanionsIdByUserIdAsync_Success_ReturnsIds()
    {
        var userId = Guid.NewGuid();
        var companions = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _repoMock.Setup(r => r.GetAllCompanionsIdByUserIdAsync(userId))
            .ReturnsAsync(Result<IEnumerable<Guid>>.Success(companions));

        var result = await _service.GetAllCompanionsIdByUserIdAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(companions, result.Value?.Select(dto => dto.Id));
    }

    [Fact]
    public async Task UpdateAsync_SenderUpdates_Success()
    {
        var senderId = Guid.NewGuid();
        var message = CreateTestMessage(senderId, Guid.NewGuid());
        var updateDto = new MessagePutDto { Id = message.Id, Text = "Updated" };
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(message)!);
        
        _repoMock.Setup(r => r.UpdateAsync(message.Id, 
                It.IsAny<Action<MessageEntity>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.UpdateAsync(senderId, updateDto);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_NonSenderTriesUpdate_ReturnsError()
    {
        var senderId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = CreateTestMessage(senderId, Guid.NewGuid());
        var updateDto = new MessagePutDto { Id = message.Id, Text = "Updated" };
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(null)!);

        var result = await _service.UpdateAsync(otherUserId, updateDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task DeleteAsync_SenderDeletes_Success()
    {
        var senderId = Guid.NewGuid();
        var message = CreateTestMessage(senderId, Guid.NewGuid());
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(message)!);
        
        _repoMock.Setup(r => r.DeleteAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result.Success());

        var result = await _service.DeleteAsync(senderId, message.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_NonSenderTriesDelete_ReturnsError()
    {
        var senderId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = CreateTestMessage(senderId, Guid.NewGuid());
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(null)!);

        var result = await _service.DeleteAsync(otherUserId, message.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(null)!);

        var result = await _service.GetByIdAsync(userId, messageId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error?.ErrorType);
    }

    [Fact]
    public async Task UpdateAsync_MessageNotFound_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var updateDto = new MessagePutDto { Id = Guid.NewGuid() };
        
        _repoMock.Setup(r => r.GetByFilterAsync(
                It.IsAny<Expression<Func<MessageEntity, bool>>>()))
            .ReturnsAsync(Result<Message?>.Success(null)!);

        var result = await _service.UpdateAsync(userId, updateDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error?.ErrorType);
    }

    [Fact]
    public async Task AddAsync_RepositoryError_ReturnsFailure()
    {
        var messageAddDto = new MessageAddDto();
        var message = CreateTestMessage(Guid.NewGuid(), Guid.NewGuid());
        var error = new Error(ErrorType.ServerError, "DB error");
        
        _mapperMock.Setup(m => m.Map<Message>(messageAddDto)).Returns(message);
        _repoMock.Setup(r => r.AddAsync(message)).ReturnsAsync(Result.Failure(error));

        var result = await _service.AddAsync(messageAddDto);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetAllCompanionsIdByUserIdAsync_RepositoryError_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var error = new Error(ErrorType.ServerError, "DB error");
        
        _repoMock.Setup(r => r.GetAllCompanionsIdByUserIdAsync(userId))
            .ReturnsAsync(Result<IEnumerable<Guid>>.Failure(error));

        var result = await _service.GetAllCompanionsIdByUserIdAsync(userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}