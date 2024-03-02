using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Common.Extensions;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public class ToolRepository : GenericRepository<DbToolDTO>
    {
        private readonly string _connectionString;

        public ToolRepository(string connectionString) : base(connectionString)
        {
            _connectionString = connectionString;
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
                            p.Enum AS propertiesEnum,
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
                            p.Enum AS propertiesEnum,
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

        //public async Task<IEnumerable<Tool>> GetProfileToolsAsync(int profileId)
        //{
        //    try
        //    {
        //        using (var connection = new SqlConnection(_connectionString))
        //        {
        //            await connection.OpenAsync();

        //            // Assuming the names of your tables and columns
        //            var query = @"
        //                SELECT t.*
        //                FROM tools t
        //                INNER JOIN profileTools pt ON t.Id = pt.ToolID
        //                WHERE pt.ProfileId = @ProfileId";

        //            using (var command = new SqlCommand(query, connection))
        //            {
        //                command.Parameters.AddWithValue("@ProfileId", profileId);

        //                using (var reader = await command.ExecuteReaderAsync())
        //                {
        //                    var tools = new List<Tool>();

        //                    while (await reader.ReadAsync())
        //                    {
        //                        tools.Add(MapToolFromReader(reader)); // Assuming you have a MapFromReader method for Tool
        //                    }

        //                    return tools;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public async Task<Tool> MapToolFromReader(SqlDataReader reader)
        {
            var tool = new DbToolDTO
            {
                Id = (int)reader["Id"],
                Name = (string)reader["Name"],
                Type = (string)reader["Type"], // this always equals function
                Description = reader["Description"] as string
            };

            // Create a dictionary to store properties
            var propertyList = new List<DbPropertyDTO>();

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
                    var propertyEnum = reader["propertiesEnum"] as string;
                    var propertyDescription = reader["propertiesDescription"] as string;

                    var propDto = new PropertyDTO()
                    {
                        Id = propertyId,
                        Type = propertyType,
                        Description = propertyDescription
                    };

                    if (propertyEnum != null)
                    {
                        propDto.Enum = propertyEnum.ToStringArray();
                    }

                    // Create a PropertyDTO and add it to the dictionary
                    propertyList.Add(new DbPropertyDTO(propertyName, propDto));
                }
            } while (await reader.ReadAsync());
            return new Tool(tool, propertyList);
        }
    }

    













    //public class Repository<T> : IRepository<T> where T : class
    //{
    //    private readonly DbContext _context;
    //    private readonly DbSet<T> _dbSet;

    //    public Repository(DbContext context)
    //    {
    //        _context = context ?? throw new ArgumentNullException(nameof(context));
    //        _dbSet = _context.Set<T>();
    //    }

    //    // Remove ORM
    //    public async Task<T> GetById(int id)
    //    {
    //        return await _dbSet.FindAsync(id);
    //    }

    //    public async Task<T> GetByColumn(string columnName, string value)
    //    {
    //        var entity = await _dbSet
    //            .Where(profile => EF.Property<string>(profile, columnName) == value)
    //            .FirstOrDefaultAsync();
    //        return entity;
    //    }

    //    public async Task<IEnumerable<T>> GetAll()
    //    {
    //        return await _dbSet.ToListAsync();
    //    }

    //    public async Task Add(T entity)
    //    {
    //        await _dbSet.AddAsync(entity);
    //        await _context.SaveChangesAsync();
    //    }

    //    public async Task Update(T existingEntity, T entity)
    //    {
    //        var entityId = existingEntity.GetType().GetProperty("Id").GetValue(existingEntity);
    //        var entityProperties = entity.GetType().GetProperties().Where(p => 
    //            p.Name != "Id" && 
    //            p.Name != "Name" && 
    //            p.GetValue(entity) != null && 
    //            p.Name != "Response_Format" // probably just add this to JsonIgnore if this is even needed
    //            );

    //        foreach (var property in entityProperties)
    //        {
    //            _context.Entry(existingEntity).Property(property.Name).CurrentValue = property.GetValue(entity);
    //        }
    //        await _context.SaveChangesAsync();
    //    }

    //    public async Task Delete(T entity)
    //    {
    //        _dbSet.Remove(entity);
    //        await _context.SaveChangesAsync();
    //    }
    //}
}