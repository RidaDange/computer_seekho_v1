using AutoMapper;
using ComputerSeekho.Repository;
using Microsoft.Extensions.Logging;

namespace ComputerSeekho.Service;

/// <summary>
/// Requirement 8, at the service layer — the CRUD every master table needs
/// and none of them needs to write.
///
/// Courses, categories, recruiters, banners, testimonials, closure reasons
/// and the rest are all this class with a different type argument. Entities
/// that carry real business rules get their own service which *calls* this
/// one rather than reimplementing it — see EmployeeService, which is
/// requirement 9.
/// </summary>
public class GenericService<TEntity, TDto> : IGenericService<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    protected readonly IGenericRepository<TEntity> Repository;
    protected readonly IMapper Mapper;
    protected readonly ILogger Logger;

    public GenericService(
        IGenericRepository<TEntity> repository,
        IMapper mapper,
        ILogger<GenericService<TEntity, TDto>> logger)
    {
        Repository = repository;
        Mapper = mapper;
        Logger = logger;
    }

    public virtual async Task<IEnumerable<TDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await Repository.GetAllAsync(ct);
        return Mapper.Map<IEnumerable<TDto>>(entities);
    }

    public virtual async Task<TDto?> GetByIdAsync(object id, CancellationToken ct = default)
    {
        var entity = await Repository.GetByIdAsync(id, ct);
        return entity is null ? null : Mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto> CreateAsync<TRequest>(TRequest request, CancellationToken ct = default)
        where TRequest : class
    {
        var entity = Mapper.Map<TEntity>(request);
        var saved = await Repository.AddAsync(entity, ct);

        Logger.LogInformation("Created {Entity}", typeof(TEntity).Name);
        return Mapper.Map<TDto>(saved);
    }

    public virtual async Task<TDto?> UpdateAsync<TRequest>(object id, TRequest request, CancellationToken ct = default)
        where TRequest : class
    {
        var existing = await Repository.GetByIdAsync(id, ct);
        if (existing is null) return null;

        // Mapped onto the tracked instance rather than replacing it. Mapping
        // to a fresh object and saving that would null out every column the
        // request doesn't carry — the classic way a partial update wipes
        // half a row.
        Mapper.Map(request, existing);
        await Repository.UpdateAsync(existing, ct);

        Logger.LogInformation("Updated {Entity} {Id}", typeof(TEntity).Name, id);
        return Mapper.Map<TDto>(existing);
    }

    public virtual async Task<bool> DeleteAsync(object id, CancellationToken ct = default)
    {
        var deleted = await Repository.DeleteAsync(id, ct);

        if (deleted) Logger.LogInformation("Deleted {Entity} {Id}", typeof(TEntity).Name, id);
        else Logger.LogWarning("Delete missed — no {Entity} with id {Id}", typeof(TEntity).Name, id);

        return deleted;
    }
}
