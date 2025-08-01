﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing agent profiles in the database.
    /// </summary>
    public interface IProfileRepository
    {
        /// <summary>
        /// Retrieves a profile from the database by name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>The matching profile, or null if no results are found.</returns>
        Task<DbProfile?> GetByNameAsync(string name);

        /// <summary>
        /// Retrieves a profile from the database by id.
        /// </summary>
        /// <param name="id">The Id of the profile.</param>
        /// <returns>The profile.</returns>
        Task<DbProfile?> GetAsync(int id);

        /// <summary>
        /// Retrieves all profiles from the database.
        /// </summary>
        /// <param name="count">The number of profiles to retrieve.</param>
        /// <param name="page">The page number to offset the results by.</param>
        /// <returns>A list of profiles.</returns>
        Task<IEnumerable<DbProfile>> GetAllAsync(int? count = null, int? page = null);

        /// <summary>
        /// Updates an existing profile in the database.
        /// </summary>
        /// <param name="updateProfileDto">The new definition of the profile.</param>
        /// <returns>The updated profile.</returns>
        Task<DbProfile> UpdateAsync(DbProfile updateProfileDto);

        /// <summary>
        /// Adds a new profile to the database.
        /// </summary>
        /// <param name="updateProfileDto">The newly added profile.</param>
        /// <returns>The new profile.</returns>
        Task<DbProfile> AddAsync(DbProfile updateProfileDto);

        /// <summary>
        /// Deletes a profile from the database.
        /// </summary>
        /// <param name="profile">The profile to delete.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteAsync(DbProfile profile);
    }
}
