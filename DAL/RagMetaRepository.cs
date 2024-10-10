using IntelligenceHub.API.DTOs.DataAccessDTOs;
using OpenAICustomFunctionCallingAPI.API.MigratedDTOs.RAG;

namespace IntelligenceHub.DAL
{
    public class RagMetaRepository : GenericRepository<IndexMetadata>
    {
        public RagMetaRepository(string connectionString) : base(connectionString)
        {
        }
    }
}