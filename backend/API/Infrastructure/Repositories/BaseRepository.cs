namespace API.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Generic base repository providing standard CRUD operations using Entity Framework Core.
/// All specialized repositories inherit from this class.
/// </summary>
/// <typeparam name="T">Entity type managed by this repository.</typeparam>
public abstract class BaseRepository<T> where T : class
{
    /// <summary>
    /// DbContext instance for database operations.
    /// </summary>
    protected readonly DbContext Context;

    /// <summary>
    /// DbSet for the entity type.
    /// </summary>
    protected readonly DbSet<T> DbSet;

    /// <summary>
    /// Initializes a new instance of the BaseRepository class.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    protected BaseRepository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>Entity if found, null otherwise.</returns>
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await DbSet.FindAsync(id);
    }

    /// <summary>
    /// Gets all entities from the database.
    /// </summary>
    /// <returns>Collection of all entities.</returns>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Adds a new entity to the database.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity.</returns>
    public virtual async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>Completed task.</returns>
    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an entity from the database.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>Completed task.</returns>
    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>Number of entities affected.</returns>
    public virtual async Task<int> SaveChangesAsync()
    {
        return await Context.SaveChangesAsync();
    }
}
