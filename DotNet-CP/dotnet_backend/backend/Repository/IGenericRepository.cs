using System.Linq.Expressions;

namespace ComputerSeekho.Repository;

/// <summary>
/// Requirement 7 — the generic CRUD contract.
///
/// One interface serves every entity, which is what stops the project
/// needing twenty near-identical repositories. The type parameter is
/// constrained to a class only: constraining it to some IEntity base would
/// force a shared base type onto entities that have nothing else in common,
/// and their primary keys are already discoverable by EF Core.
///
/// Returns IEnumerable rather than IQueryable on purpose. Handing an
/// IQueryable to a controller lets database concerns leak upward — the
/// query would then be composed and executed somewhere that has no idea a
/// database is involved, and would fail after the context is disposed.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Finds by primary key. Null when there is no such row.</summary>
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);

    /// <summary>
    /// Arbitrary filtering, so callers needing "the active ones" don't have
    /// to fetch everything and filter in memory.
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>
    /// Like FindAsync, but eagerly loads the named relationships.
    ///
    /// Needed because a DTO sometimes carries a field from another table —
    /// CourseDto.CategoryName being the obvious case. Without the include
    /// the navigation property is null and the field silently comes back
    /// empty, which is worse than an error because nothing complains.
    ///
    /// Includes are passed explicitly rather than the repository guessing:
    /// loading every relationship on every read is how a list endpoint ends
    /// up dragging half the database back with it.
    /// </summary>
    Task<IEnumerable<T>> FindWithIncludesAsync(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>[] includes,
        CancellationToken ct = default);

    /// <summary>Single row by predicate, with relationships loaded.</summary>
    Task<T?> FirstWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>[] includes,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task<T> AddAsync(T entity, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>Returns false when the id matched nothing.</summary>
    Task<bool> DeleteAsync(object id, CancellationToken ct = default);
}
