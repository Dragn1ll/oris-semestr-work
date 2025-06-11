using Application.Utils;
using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface IMessageRepository : IRepository<Message, MessageEntity>
{
    Task<Result<IEnumerable<Guid>>> GetAllCompanionsIdByUserIdAsync(Guid userId);
}