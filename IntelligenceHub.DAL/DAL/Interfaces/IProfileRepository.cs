﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IProfileRepository
    {
        Task<DbProfile?> GetByNameAsync(string name);
        Task<IEnumerable<DbProfile>> GetAllAsync(int? count = null, int? page = null);
        Task<int> UpdateAsync(DbProfile existingProfile, DbProfile updateProfileDto);
        Task<DbProfile> AddAsync(DbProfile updateProfileDto);
        Task<int> DeleteAsync(DbProfile profile);
    }
}
