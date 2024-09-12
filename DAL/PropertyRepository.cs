using Nest;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace IntelligenceHub.DAL
{
    public class PropertyRepository : GenericRepository<DbPropertyDTO>
    {
        public PropertyRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<List<DbPropertyDTO>> GetToolProperties(int Id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT * FROM {_table}
                        WHERE ToolId = @Id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("Id", Id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var propertyList = new List<DbPropertyDTO>();
                            while (await reader.ReadAsync())
                            {
                                propertyList.Add(MapFromReader<DbPropertyDTO>(reader));
                            }
                            return propertyList;
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<DbPropertyDTO> UpdatePropertyAsync(DbPropertyDTO existingEntity, DbPropertyDTO entity)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var setClause = string.Join(", ", typeof(DbPropertyDTO).GetProperties()
                        .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "ToolId") // Exclude Id and Name properties
                        .Select(p => $"{p.Name} = @{p.Name}"));

                    var query = $@"
                        UPDATE {_table} SET {setClause} 
                        WHERE Name = @Name
                        AND ToolID = @ToolID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        foreach (var property in typeof(DbPropertyDTO).GetProperties())
                        {
                            // Exclude Id and Name properties from being updated
                            if (property.Name != "Id" && property.Name != "Name" && property.Name != "ToolId")
                            {
                                var paramName = $"@{property.Name}";
                                var value = property.GetValue(entity) ?? DBNull.Value;
                                command.Parameters.AddWithValue(paramName, value);
                            }
                        }

                        command.Parameters.AddWithValue("@Name", existingEntity.Name);
                        command.Parameters.AddWithValue("@ToolID", existingEntity.ToolId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapFromReader<DbPropertyDTO>(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}