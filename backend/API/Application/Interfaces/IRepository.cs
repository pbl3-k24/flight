namespace API.Application.Interfaces;

/// <summary>
/// Generic repository interface providing standard CRUD operations.
/// This is the base interface for all repository implementations.
/// </summary>
/// <typeparam name="T">Entity type managed by this repository.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>Entity if found, null otherwise.</returns>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all entities from the database.
    /// </summary>
    /// <returns>Collection of all entities.</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Adds a new entity to the database.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity.</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>Completed task.</returns>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity from the database.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>Completed task.</returns>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>Number of entities affected.</returns>
    Task<int> SaveChangesAsync();
}
