using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL
{
    public class IndexMetaRepository : GenericRepository<DbIndexMetadata>
    {
        public IndexMetaRepository(string connectionString) : base(connectionString)
        {
        }
    }
}