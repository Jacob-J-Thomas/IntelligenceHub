using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.DAL
{
    public class RagMetaRepository : GenericRepository<IndexMetadata>
    {
        public RagMetaRepository(string connectionString) : base(connectionString)
        {
        }
    }
}