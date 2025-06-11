using System.Collections;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Utils;
using AutoMapper;
using Domain.Entities;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.DataAccess.Repositories;

public class PostRepository(AppDbContext context, IMapper mapper) : 
    Repository<Post, PostEntity>(context, mapper), IPostRepository
{
    private readonly IMapper _mapper = mapper;

    public async Task<Result<IEnumerable<Post>>> GetAllByNewAsync()
    {
        try
        {
            var posts = (await DbSet
                .AsNoTracking()
                .OrderByDescending(pe => pe.DateTime)
                .ToListAsync())
                .Select(_mapper.Map<Post>);
            
            return Result<IEnumerable<Post>>.Success(posts);
        }
        catch (Exception)
        {
            return Result<IEnumerable<Post>>.Failure(new Error(ErrorType.ServerError,
                "Не удалось получить посты"));
        }
    }
}