using System;
using System.Collections.Generic;
using System.Linq;
using Nest;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public interface IRepository<T>
    {
        Task<T> GetByNameAsync(string name);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);

        // probably return T for below?
        Task<int> UpdateAsync(T existingEntity, T entity);
        Task<int> DeleteAsync(T entity);
    }
}