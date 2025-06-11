using Application.Dto_s;
using Application.Dto_s.Message;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.MainServices;
using Application.Utils;
using AutoMapper;
using Domain.Models;

namespace Application.Services.MainServices;

public class MessageService(
    IMessageRepository messageRepository,
    IMapper mapper
    ) : IMessageService
{
    public async Task<Result<IdDto>> AddAsync(MessageAddDto messageAddDto)
    {
        try
        {
            var model = mapper.Map<Message>(messageAddDto);
            
            var addResult = await messageRepository.AddAsync(model);
            
            return !addResult.IsSuccess
                ? Result<IdDto>.Failure(addResult.Error) 
                : Result<IdDto>.Success(mapper.Map<IdDto>(model));
        }
        catch (Exception)
        {
            return Result<IdDto>.Failure(new Error(ErrorType.ServerError, 
                "Произошла ошибка при добавлении собщения в систему"));
        }
    }

    public async Task<Result<MessageInfoDto>> GetByIdAsync(Guid userId, Guid messageId)
    {
        try
        {
            var getResult = await messageRepository.GetByFilterAsync(m => 
                m.Id == messageId && (m.RecipientId == userId || m.SenderId == userId));

            return getResult switch
            {
                { IsSuccess: true, Value: not null } => Result<MessageInfoDto>.Success(
                    mapper.Map<MessageInfoDto>(getResult.Value)),
                { IsSuccess: true, Value: null } => Result<MessageInfoDto>.Failure(new Error(ErrorType.NotFound,
                    "Такое сообщение не найдено")),
                _ => Result<MessageInfoDto>.Failure(getResult.Error)
            };
        }
        catch (Exception)
        {
            return Result<MessageInfoDto>.Failure(new Error(ErrorType.ServerError, 
                "Не найдено сообщение"));
        }
    }

    public async Task<Result<IEnumerable<MessageInfoDto>>> GetAllByUsersIdAsync(Guid firstId, Guid secondId)
    {
        try
        {
            var getResult = (await messageRepository.GetAllByFilterAsync(m =>
                (m.SenderId == firstId && m.RecipientId == secondId)
                || (m.SenderId == secondId && m.RecipientId == firstId)));
            
            return !getResult.IsSuccess
                ? Result<IEnumerable<MessageInfoDto>>.Failure(getResult.Error)
                : Result<IEnumerable<MessageInfoDto>>.Success(
                    mapper.Map<IEnumerable<MessageInfoDto>>(getResult.Value));
        }
        catch (Exception)
        {
            return Result<IEnumerable<MessageInfoDto>>.Failure(new Error(ErrorType.ServerError, 
                "Не удалось получить сообщения переписки"));
        }
    }

    public async Task<Result<IEnumerable<IdDto>>> GetAllCompanionsIdByUserIdAsync(Guid userId)
    {
        try
        {
            var getResult = await messageRepository.GetAllCompanionsIdByUserIdAsync(userId);

            return !getResult.IsSuccess
                ? Result<IEnumerable<IdDto>>.Failure(getResult.Error)
                : Result<IEnumerable<IdDto>>.Success(getResult.Value!.Select(m => new IdDto
                {
                    Id = m
                }));
        }
        catch (Exception)
        {
            return Result<IEnumerable<IdDto>>.Failure(new Error(ErrorType.ServerError, 
                "Не удалось получить собеседников"));
        }
    }

    public async Task<Result> UpdateAsync(Guid userId, MessagePutDto messagePutDto)
    {
        try
        {
            if (!await HasMessageAccess(userId, messagePutDto.Id))
                return Result.Failure(new Error(ErrorType.BadRequest, 
                    "Нет прав для изменения сообщения"));
            
            var putResult = await messageRepository.UpdateAsync(messagePutDto.Id, 
                m => m.Text = messagePutDto.Text);
            
            return !putResult.IsSuccess
                ? Result.Failure(putResult.Error)
                : Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                "Не удалось изменить сообщение"));
        }
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid messageId)
    {
        try
        {
            if (!await HasMessageAccess(userId, messageId))
                return Result.Failure(new Error(ErrorType.BadRequest, 
                    "Нет прав для удаления сообщения"));
            
            var deleteResult = await messageRepository.DeleteAsync(m => m.Id == messageId);
            
            return !deleteResult.IsSuccess
                ? Result.Failure(deleteResult.Error)
                : Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                "Не удалось удалить сообщение"));
        }
    }
    
    private async Task<bool> HasMessageAccess(Guid userId, Guid messageId) =>
        await messageRepository.GetByFilterAsync(m => m.Id == messageId 
                                                       && m.SenderId == userId)
        is {IsSuccess : true , Value : not null};
}