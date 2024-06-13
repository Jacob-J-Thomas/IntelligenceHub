using System;
using System.Collections.Generic;
using System.Linq;
using Nest;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public interface IGenericRepository<T>
    {
        Task<T> GetByNameAsync(string name);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<int> UpdateAsync(T existingEntity, T entity);
        Task<int> DeleteAsync(T entity);
    }
}