using System.Linq.Expressions;
using EPCApp.Database.Entities;

namespace EPCApp.Repositories.Contracts;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}