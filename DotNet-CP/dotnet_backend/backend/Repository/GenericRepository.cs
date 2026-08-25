using System.Linq.Expressions;
using ComputerSeekho.Models;
using Microsoft.EntityFrameworkCore;

namespace ComputerSeekho.Repository;

/// <summary>
/// Requirement 8 — one implementation reused by every entity.
///
/// EF Core's DbSet already is a repository, so this layer earns its place
/// only by giving the services a narrow, testable seam: a service depending
/// on IGenericRepository&lt;Course&gt; can be unit tested with a Moq stub and
/// no database at all, which is what the NUnit tests rely on.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    // AsNoTracking on every read path. These entities are mapped to DTOs and
    // returned, never modified, so the change tracker would hold them for
    // nothing — measurable on list endpoints and a common cause of a later
    // SaveChanges writing something nobody asked it to.
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking().ToListAsync(ct);

    public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<IEnumerable<T>> FindWithIncludesAsync(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>[] includes,
        CancellationToken ct = default) =>
        await BuildQuery(predicate, includes).ToListAsync(ct);

    public async Task<T?> FirstWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>[] includes,
        CancellationToken ct = default) =>
        await BuildQuery(predicate, includes).FirstOrDefaultAsync(ct);

    /// <summary>
    /// Composes the filter and the includes onto one IQueryable, which EF
    /// Core turns into a single SQL statement with joins. Applying the
    /// includes one at a time in a loop rather than chaining them is what
    /// keeps this working for any number of relationships.
    /// </summary>
    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet.AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return predicate is null ? query : query.Where(predicate);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null
            ? await DbSet.CountAsync(ct)
            : await DbSet.CountAsync(predicate, ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(object id, CancellationToken ct = default)
    {
        var entity = await DbSet.FindAsync([id], ct);
        if (entity is null) return false;

        DbSet.Remove(entity);
        await Context.SaveChangesAsync(ct);
        return true;
    }
}
