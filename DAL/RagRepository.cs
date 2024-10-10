using IntelligenceHub.API.DTOs.RAG;
using System.Data.SqlClient;

namespace IntelligenceHub.DAL
{
    public class RagRepository : GenericRepository<RagDocument>
    {
        public RagRepository(string connectionString) : base(connectionString)
        {
        }


        public async Task<int> GetRagIndexLengthAsync(string tableName)
        {
            // validate table name

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = $@"SELECT COUNT(*) FROM [{tableName}]";

                using (var command = new SqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return (int)result;
                }
            }
        }

        public async Task<RagDocument?> GetDocumentAsync(string tableName, string title)
        {
            var ragDTOs = new List<RagDocument>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var sql = $@"SELECT * FROM [{tableName}] WHERE Title = @Title;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapFromReader<RagDocument>(reader); // Assuming you have this method
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> CreateIndexAsync(string indexName)
        {
            // validate before sql formatting here

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Can chunk size be parameratized?
                // Also, the below sql table could be created more dynamically, but this
                // doesn't seem worth it at the moment. Instead fields will be left null
                // until otherwise indicated they should be used
                var query = $@"CREATE TABLE [{indexName}] (
                                   Id INT PRIMARY KEY IDENTITY,
                                   Title NVARCHAR(255) NOT NULL,
                                   Content NVARCHAR(MAX) NOT NULL,
                                   Topic NVARCHAR(255),
                                   KeyWords NVARCHAR(255),
                                   Chunk INT NOT NULL,
                                   SourceName NVARCHAR(255),
                                   SourceLink NVARCHAR(255),
                                   PermissionGroup INT,
                                   TitleVector VARBINARY(MAX),
                                   ContentVector VARBINARY(MAX),
                                   TopicVector VARBINARY(MAX),
                                   KeywordVector VARBINARY(MAX),
                                   Created DATETIME,
                                   Modified DATETIME);";

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;// does this always return true?
                }
            }
        }

        public async Task<bool> DeleteIndexAsync()
        {
            // Validate table name here

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = $@"DROP TABLE IF EXISTS [{_table}]";

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }
    }
}