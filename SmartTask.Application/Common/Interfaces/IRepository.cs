using SmartTask.Domain.Common;
using System.Linq.Expressions;

namespace SmartTask.Application.Common.Interfaces;

/// <summary>
/// Generic repository — her entity için ayrı interface yazmaya gerek yok.
/// IRepository<TaskItem> veya IRepository<User> şeklinde kullanılır.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}

/// <summary>
/// Unit of Work — tüm repo değişikliklerini tek transaction'da kaydeder.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
