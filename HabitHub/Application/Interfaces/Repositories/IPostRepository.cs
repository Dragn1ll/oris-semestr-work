using Application.Utils;
using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface IPostRepository : IRepository<Post, PostEntity>
{
    Task<Result<IEnumerable<Post>>> GetAllByNewAsync();
}