using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL
{
    public class IndexMetaRepository : GenericRepository<DbIndexMetadata>, IIndexMetaRepository
    {
        public IndexMetaRepository(Settings settings) : base(settings.DbConnectionString)
        {
        }

        public async Task<IEnumerable<DbIndexMetadata>> GetAllAsync(int? count = null, int? page = null)
        {
            return await base.GetAllAsync(count, page);
        }

        public async Task<DbIndexMetadata?> GetByNameAsync(string name)
        {
            return await base.GetByNameAsync(name);
        }

        public async Task<DbIndexMetadata> AddAsync(DbIndexMetadata entity)
        {
            return await base.AddAsync(entity);
        }

        public async Task<int> DeleteAsync(DbIndexMetadata entity, string name)
        {
            return await base.DeleteAsync(entity, name);
        }
    }
}