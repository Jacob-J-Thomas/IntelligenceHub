using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public class RagRepository : GenericRepository<RagChunk>
    {
        public RagRepository(string connectionString) : base(connectionString)
        {
        }

        public void SetTable(string tableName)
        {
            _table = tableName;
        }

        // I can't tell if this is working... Seems like it might be, but the match in language has to be pretty exact at the moment
        // Revisit once you have a DB with more documents loaded
        public async Task<List<RagChunk>> CosineSimilarityQueryAsync(string targetColumn, byte[] queryEmbeddingBinary, int nDocs)
        {
            var ragDTOs = new List<RagChunk>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var query = $@"
                                SELECT TOP(@NumberDocs) *,
                                    dbo.CalculateCosineSimilarity({targetColumn}Vector,  @queryEmbeddingBinary) 
                                    AS SimilarityScore
                                FROM 
                                    [{_table}]
                                ORDER BY 
                                    SimilarityScore DESC;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@QueryEmbeddingBinary", queryEmbeddingBinary);
                    //cmd.Parameters.AddWithValue("@TargetNorm", targetColumn + "Norm");
                    //cmd.Parameters.AddWithValue("@QueryNorm", queryNorm);
                    cmd.Parameters.AddWithValue("@NumberDocs", nDocs);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var ragDTO = MapFromReader<RagChunk>(reader); // Assuming you have this method
                            ragDTOs.Add(ragDTO);
                        }
                    }
                }
            }
            return ragDTOs;
        }


        // again, choose which columns to perform the search against
        public async Task<List<RagChunk>> BM25QueryAsync(string tableName, string query)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateAccessCountAsync(string tableName, int id)
        {
            // Validate table name here

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = $@"UPDATE {tableName} SET AccessCount = AccessCount + 1 WHERE Id = @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
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

        public async Task<List<RagChunk>> GetAllChunksAsync(string tableName, string title)
        {
            var ragDTOs = new List<RagChunk>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var sql = $@"SELECT * FROM [{tableName}] WHERE Title = @Title;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var ragDTO = MapFromReader<RagChunk>(reader); // Assuming you have this method
                            ragDTOs.Add(ragDTO);
                        }
                    }
                }
            }
            return ragDTOs;
        }

        public async Task<bool> CreateIndexAsync(RagIndexMetaDataDTO index)
        {
            // validate before sql formatting here

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Can chunk size be parameratized?
                // Also, the below sql table could be created more dynamically, but this
                // doesn't seem worth it at the moment. Instead fields will be left null
                // until otherwise indicated they should be used
                var query = $@"CREATE TABLE [{_table}] (
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
                                   TitleVectorNorm FLOAT,
                                   ContentVectorNorm FLOAT,
                                   TopicVectorNorm FLOAT,
                                   KeywordVectorNorm FLOAT,
                                   CreatedDate DATETIME,
                                   ModifiedDate DATETIME,
                                   AccessCount INT);";

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