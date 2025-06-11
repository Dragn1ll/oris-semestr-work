using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Utils;
using AutoMapper;
using Domain.Entities;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.DataAccess.Repositories;

public class MessageRepository(AppDbContext context, IMapper mapper) : 
    Repository<Message, MessageEntity>(context, mapper), IMessageRepository
{
    public async Task<Result<IEnumerable<Guid>>> GetAllCompanionsIdByUserIdAsync(Guid userId)
    {
        try
        {
            var companions = await DbSet
                .AsNoTracking()
                .Where(m => m.SenderId == userId || m.RecipientId == userId)
                .Select(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
                .Distinct()
                .ToListAsync();
            
            return Result<IEnumerable<Guid>>.Success(companions);
        }
        catch (Exception)
        {
            return Result<IEnumerable<Guid>>.Failure(new Error(ErrorType.ServerError, 
                "Не удалось получить собеседников"));
        }
    }
}