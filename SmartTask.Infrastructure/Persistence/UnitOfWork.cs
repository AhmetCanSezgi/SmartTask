using SmartTask.Application.Common.Interfaces;
using SmartTask.Domain.Common;
using SmartTask.Infrastructure.Persistence.Repositories;

namespace SmartTask.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext context) => _context = context;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            repo = new GenericRepository<TEntity>(_context);
            _repositories[type] = repo;
        }
        return (IRepository<TEntity>)repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}
