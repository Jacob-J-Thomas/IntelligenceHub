namespace IntelligenceHub.DAL
{
    public interface IGenericRepository<T>
    {
        Task<T> GetByNameAsync(string name);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity, string? overrideTable = null);
        Task<int> UpdateAsync(T existingEntity, T entity, string? overrideTable = null);
        Task<int> DeleteAsync(T entity, string? overrideTable = null);
    }
}