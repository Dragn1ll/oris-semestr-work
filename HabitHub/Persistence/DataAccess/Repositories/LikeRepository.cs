using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class LikeRepository(AppDbContext context, IMapper mapper) : 
    Repository<Like, LikeEntity>(context, mapper), ILikeRepository
{
    
}