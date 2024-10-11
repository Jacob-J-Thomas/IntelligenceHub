using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.DAL.Models;
using System.Data.SqlClient;

namespace IntelligenceHub.DAL
{
    // This repository is unique in that the table name must be provided for all methods
    public class IndexRepository : GenericRepository<DbIndexDocument>
    {
        public IndexRepository(string connectionString) : base(connectionString)
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

        public async Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title)
        {
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
                            return MapFromReader<DbIndexDocument>(reader); // Assuming you have this method
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

                var query = $@"CREATE TABLE [{indexName}] (
                                   Id INT IDENTITY(1,1) PRIMARY KEY,
                                   Title NVARCHAR(255) NOT NULL,
                                   Content NVARCHAR(MAX) NOT NULL,
                                   Topic NVARCHAR(255),
                                   Keywords NVARCHAR(255),
                                   Source NVARCHAR(510) NOT NULL,
                                   Created DATETIMEOFFSET NOT NULL,
                                   Modified DATETIMEOFFSET NOT NULL
                               );";

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;// does this always return true?
                }
            }
        }

        public async Task<bool> DeleteIndexAsync(string table)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = $@"DROP TABLE IF EXISTS [{table}]";

                using (var command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }
    }
}