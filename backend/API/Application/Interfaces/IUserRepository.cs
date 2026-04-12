namespace API.Application.Interfaces;

using API.Domain.Entities;

/// <summary>
/// Repository interface for user data access operations.
/// Defines contracts for database operations on users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by ID from the database.
    /// </summary>
    /// <param name="id">The user ID to retrieve.</param>
    /// <returns>User entity if found, null otherwise.</returns>
    Task<User?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <returns>User entity if found, null otherwise.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Checks if a user exists by ID.
    /// </summary>
    /// <param name="id">The user ID to check.</param>
    /// <returns>True if user exists, false otherwise.</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>Number of entities affected.</returns>
    Task<int> SaveChangesAsync();
}
