using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, RefreshTokenEntity>
{
    
}