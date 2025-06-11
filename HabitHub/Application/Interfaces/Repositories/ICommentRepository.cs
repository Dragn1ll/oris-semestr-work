using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface ICommentRepository : IRepository<Comment, CommentEntity>
{
    
}