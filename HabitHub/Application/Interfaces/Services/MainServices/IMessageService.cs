using Application.Dto_s;
using Application.Dto_s.Message;
using Application.Utils;

namespace Application.Interfaces.Services.MainServices;

public interface IMessageService
{
    Task<Result<IdDto>> AddAsync(MessageAddDto messageAddDto);
    Task<Result<MessageInfoDto>> GetByIdAsync(Guid userId, Guid messageId);
    Task<Result<IEnumerable<MessageInfoDto>>> GetAllByUsersIdAsync(Guid firstId, Guid secondId);
    Task<Result<IEnumerable<IdDto>>> GetAllCompanionsIdByUserIdAsync(Guid userId);
    Task<Result> UpdateAsync(Guid userId, MessagePutDto messagePutDto);
    Task<Result> DeleteAsync(Guid userId, Guid messageId);
}