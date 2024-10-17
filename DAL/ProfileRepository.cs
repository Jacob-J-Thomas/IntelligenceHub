using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;

namespace IntelligenceHub.DAL
{
    public class ProfileRepository : GenericRepository<DbProfile>
    {

        public ProfileRepository(string connectionString) : base(connectionString)
        {

        }

        // This is only working when tools are present at the moment, otherwise returns an empty set
        public async Task<Profile> GetByNameWithToolsAsync(string Name)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT 
                            p.*, 
                            t.Id AS ToolId, 
                            t.Name AS ToolName, 
                            t.Description AS ToolDescription, 
                            t.Required AS ToolRequired,
                            t.ExecutionUrl AS ToolExecutionUrl,
                            t.ExecutionMethod AS ToolExecutionMethod
                        FROM profiles p
                        JOIN profiletools pt ON p.Id = pt.ProfileID
                        JOIN tools t ON pt.ToolID = t.Id
                        WHERE p.[Name] = @Name";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", Name);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var dbProfile = MapFromReader<DbProfile>(reader);
                                var toolList = MapToolsFromReader(reader);

                                if (reader["Stop"] != DBNull.Value)
                                {
                                    dbProfile.Stop = (string)reader["Stop"];
                                }
                                
                                var profile = DbMappingHandler.MapFromDbProfile(dbProfile);
                                profile.Tools = new List<Tool>();

                                foreach (var tool in toolList)
                                {
                                    profile.Tools.Add(DbMappingHandler.MapFromDbTool(tool, null));
                                }
                                return profile;
                            }
                        }
                    }
                    return null; // Return null if no result is found
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private List<DbTool> MapToolsFromReader(SqlDataReader reader)
        {
            try
            {
                var id = (int)reader["ToolId"];
                var name = (string)reader["ToolName"];
                var description = (string)reader["ToolDescription"];
                var required = (string)reader["ToolRequired"];
                var url = reader["ToolExecutionUrl"] as string;
                var method = reader["ToolExecutionMethod"] as string;

                var tools = new List<DbTool>
                {
                    new DbTool
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        Required = required,
                        ExecutionUrl = url,
                        ExecutionMethod = method,
                    }
                };
                return tools;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
    }
}