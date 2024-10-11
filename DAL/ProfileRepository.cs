using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;
using System.Data.SqlClient;

namespace IntelligenceHub.DAL
{
    public class ProfileRepository : GenericRepository<DbProfile>
    {

        public ProfileRepository(string connectionString) : base(connectionString)
        {

        }

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
                            t.Required
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
            catch (Exception)
            {

                throw;
            }
        }

        private List<DbTool> MapToolsFromReader(SqlDataReader reader)
        {
            var tools = new List<DbTool>
            {
                new DbTool
                {
                    Id = (int)reader["Id"],
                    Name = (string)reader["Name"],
                    Description = (string)reader["Description"],
                    Required = (string)reader["Required"],
                    ExecutionUrl = reader["ExecutionUrl"] as string,
                    ExecutionMethod = reader["ExecutionMethod"] as string,
                }
            };
            return tools;
        }
    }
}