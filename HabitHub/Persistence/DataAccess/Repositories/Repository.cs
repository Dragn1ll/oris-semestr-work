using System.Linq.Expressions;
using Application.Enums;
using Application.Interfaces.Repositories;
using Application.Utils;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Persistence.DataAccess.Repositories;

public abstract class Repository<TModel, TEntity>(AppDbContext context, IMapper mapper) :
    IRepository<TModel, TEntity>
    where TModel : class
    where TEntity : class
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public async Task<Result> AddAsync(TModel model)
    {
        try
        {
            var entity = mapper.Map<TModel, TEntity>(model);

            await DbSet.AddAsync(entity);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при добавлении сущности: {ex.InnerException?.Message ?? ex.Message}"));
        }
    }

    public async Task<Result<IEnumerable<TModel>>> GetAllByFilterAsync(Expression<Func<TEntity, bool>> filter)
    {
        try
        {
            var entities = await DbSet.AsNoTracking().Where(filter).ToListAsync();
            var models = entities.Select(mapper.Map<TEntity, TModel>);
            
            return Result<IEnumerable<TModel>>.Success(models);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<TModel>>.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при получении списка сущностей: {ex.Message}"));
        }
    }

    public async Task<Result<TModel>> GetByFilterAsync(Expression<Func<TEntity, bool>> filter)
    {
        try
        {
            var entity = await DbSet.AsNoTracking().FirstOrDefaultAsync(filter);

            var model = mapper.Map<TEntity, TModel>(entity!);

            return Result<TModel>.Success(model);
        }
        catch (Exception ex)
        {
            return Result<TModel>.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при получении сущности: {ex.Message}"));
        }
    }

    public async Task<Result> UpdateAsync(Guid id, Action<TEntity> action)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity == null)
            {
                return Result.Failure(new Error(ErrorType.NotFound, "Сущность не найдена"));
            }

            action(entity);
            DbSet.Update(entity);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при обновлении сущности: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var deletedRows = await DbSet.Where(predicate).ExecuteDeleteAsync();

            return deletedRows == 0
                ? Result.Failure(new Error(ErrorType.BadRequest, 
                    "Не найдены сущности для удаления"))
                : Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorType.ServerError, 
                $"Ошибка при удалении сущности: {ex.Message}"));
        }
    }
}