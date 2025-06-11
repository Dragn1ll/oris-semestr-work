using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User, UserEntity>
{
    
}