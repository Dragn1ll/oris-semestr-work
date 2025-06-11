using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class CommentRepository(AppDbContext context, IMapper mapper) : 
    Repository<Comment, CommentEntity>(context, mapper), ICommentRepository
{
    
}