﻿using IntelligenceHub.DAL.DTOs;
using System.Data.SqlClient;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;

namespace IntelligenceHub.DAL
{
    public class ToolRepository : GenericRepository<DbTool>
    {
        public ToolRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<Tool> GetToolByNameAsync(string name)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Assuming the names of your tables and columns
                    var query = @"
                        SELECT 
                            t.*,
                            p.Id AS propertiesId,
                            p.Name AS propertiesName,
                            p.Type AS propertiesType,
                            p.ToolId AS propertiesToolId,
                            p.Description AS propertiesDescription
                        FROM tools AS t
                        LEFT JOIN properties AS p ON t.Id = p.ToolID
                        WHERE t.Name = @Name;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return await MapToolFromReader(reader);
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Tool> GetToolByIdAsync(int Id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Assuming the names of your tables and columns
                    var query = @"
                        SELECT 
                            t.*,
                            p.Id AS propertiesId,
                            p.Name AS propertiesName,
                            p.Type AS propertiesType,
                            p.ToolId AS propertiesToolId,
                            p.Description AS propertiesDescription
                        FROM tools AS t
                        LEFT JOIN properties AS p ON t.Id = p.ToolID
                        WHERE t.Id = @Id;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return await MapToolFromReader(reader);
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<string>> GetProfileToolsAsync(string name)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Assuming the names of your tables and columns
                    var query = @"
                        SELECT t.Name
                        FROM tools t
                        INNER JOIN profileTools pt ON t.Id = pt.ProfileID
                        INNER JOIN profiles p ON pt.ToolID = p.Id
                        WHERE p.Name = @Name;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var profileNames = new List<string>();

                            while (await reader.ReadAsync())
                            {
                                profileNames.Add((string)reader["Name"]); // Assuming you have a MapFromReader method for Tool
                            }

                            return profileNames;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<string>> GetToolProfilesAsync(string name)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Assuming the names of your tables and columns
                    var query = @"
                        SELECT p.Name
                        FROM profiles p
                        INNER JOIN profileTools pt ON p.Id = pt.ProfileID
                        INNER JOIN tools t ON pt.ToolID = t.Id
                        WHERE t.Name = @Name;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var profileNames = new List<string>();

                            while (await reader.ReadAsync())
                            {
                                profileNames.Add((string)reader["Name"]); // Assuming you have a MapFromReader method for Tool
                            }

                            return profileNames;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Tool> MapToolFromReader(SqlDataReader reader)
        {
            var tool = new DbTool
            {
                Id = (int)reader["Id"],
                Name = (string)reader["Name"],
                Description = reader["Description"] as string
            };

            // Create a dictionary to store properties
            var propertyList = new List<DbProperty>();

            // Check if the columns from the properties table are not null
            do
            {
                // Check if the columns from the properties table are not null
                if (reader["propertiesName"] != DBNull.Value)
                {
                    // Assuming "PropertyName", "PropertyType", and "PropertyDescription" are columns from the properties table
                    var propertyId = (int)reader["propertiesId"];
                    var propertyName = (string)reader["propertiesName"];
                    var propertyType = (string)reader["propertiesType"];
                    var propertyDescription = reader["propertiesDescription"] as string;

                    var propDto = new Property()
                    {
                        Id = propertyId,
                        Type = propertyType,
                        Description = propertyDescription
                    };

                    // Create a PropertyDTO and add it to the dictionary
                    propertyList.Add(DbMappingHandler.MapToDbProperty(propertyName, propDto));
                }
            } while (await reader.ReadAsync());
            return DbMappingHandler.MapFromDbTool(tool, propertyList);
        }
    }
}