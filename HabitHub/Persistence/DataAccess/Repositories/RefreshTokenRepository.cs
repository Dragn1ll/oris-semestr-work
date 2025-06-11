using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class RefreshTokenRepository(AppDbContext context, IMapper mapper) 
    : Repository<RefreshToken, RefreshTokenEntity>(context, mapper), IRefreshTokenRepository
{
    
}