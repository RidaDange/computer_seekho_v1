namespace ComputerSeekho.Service;

/// <summary>
/// The service-layer counterpart of IGenericRepository.
///
/// Two type parameters because a service, unlike a repository, deals in
/// DTOs: TEntity is what the database holds, TDto is what the API returns.
/// The mapping between them happens here, so no controller ever sees an
/// entity.
/// </summary>
public interface IGenericService<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    Task<IEnumerable<TDto>> GetAllAsync(CancellationToken ct = default);

    Task<TDto?> GetByIdAsync(object id, CancellationToken ct = default);

    Task<TDto> CreateAsync<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : class;

    Task<TDto?> UpdateAsync<TRequest>(object id, TRequest request, CancellationToken ct = default) where TRequest : class;

    Task<bool> DeleteAsync(object id, CancellationToken ct = default);
}
