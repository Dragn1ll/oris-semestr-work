using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class UserRepository(AppDbContext context, IMapper mapper) : 
    Repository<User, UserEntity>(context, mapper), IUserRepository
{
    
}