using System.Linq.Expressions;
using DiemEcommerce.Domain.Abstractions.Entities;
using DiemEcommerce.Domain.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Persistence.Repositories;

public class RepositoryBase<TDbContext, TEntity, TKey>(TDbContext dbContext)
    : IRepositoryBase<TDbContext, TEntity, TKey>, IDisposable
    where TEntity : Entity<TKey>
    where TDbContext : DbContext
{
    public void Dispose()
        => dbContext?.Dispose();

    public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        IQueryable<TEntity> items = dbContext.Set<TEntity>().AsNoTracking(); // Importance Always include AsNoTracking for Query Side
        if (includeProperties != null)
            foreach (var includeProperty in includeProperties)
                items = items.Include(includeProperty);

        if (predicate is not null)
            items = items.Where(predicate);

        return items;
    }

    public async Task<TEntity> FindByIdAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties)
        => await FindAll(null, includeProperties)
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

    public async Task<TEntity> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties)
        => await FindAll(null, includeProperties)
            .AsTracking()
            .SingleOrDefaultAsync(predicate, cancellationToken);

    public void Add(TEntity entity)
        => dbContext.Add(entity);

    public void AddRange(IEnumerable<TEntity> entities)
        => dbContext.AddRange(entities);

    public void UpdateRange(IEnumerable<TEntity> entities)
        => dbContext.UpdateRange(entities);

    public void Remove(TEntity entity)
        => dbContext.Set<TEntity>().Remove(entity);

    public void RemoveMultiple(List<TEntity> entities)
        => dbContext.Set<TEntity>().RemoveRange(entities);

    public void Update(TEntity entity)
        => dbContext.Set<TEntity>().Update(entity);
}