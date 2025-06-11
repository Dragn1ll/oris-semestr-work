using System.Linq.Expressions;
using Application.Utils;

namespace Application.Interfaces.Repositories;

public interface IRepository<TModel, TEntity>
{
    Task<Result> AddAsync(TModel model);
    Task<Result<IEnumerable<TModel>>> GetAllByFilterAsync(Expression<Func<TEntity, bool>> filter);
    Task<Result<TModel>> GetByFilterAsync(Expression<Func<TEntity, bool>> filter);
    Task<Result> UpdateAsync(Guid id, Action<TEntity> action);
    Task<Result> DeleteAsync(Expression<Func<TEntity, bool>> predicate);
}