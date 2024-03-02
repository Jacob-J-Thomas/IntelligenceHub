using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Common;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    //make this more generic
    public class ProfileToolsRepository : IAssociativeRepository<APIProfileDTO>
    {
        private readonly string _connectionString;

        public ProfileToolsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ProfileToolDTO>> GetToolIdsAsync(int profileId) // change to delete all associations by profile
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"SELECT * FROM [master].[dbo].[profileTools] WHERE ProfileId = @ProfileId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Assuming there is an Id property for the WHERE clause
                        command.Parameters.AddWithValue("@ProfileId", profileId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var associations = new List<ProfileToolDTO>();
                            while (await reader.ReadAsync())
                            {
                                associations.Add(MapAssociationsFromReader(reader));
                            }
                            return associations;
                        }
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddAssociationsByProfileIdAsync(int profileId, List<int> toolList)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    foreach (var toolId in toolList)
                    {
                        var query = @"
                            IF @ProfileID IS NOT NULL AND NOT EXISTS (
                                SELECT 1
                                FROM profileTools
                                WHERE ProfileID = @ProfileID
                                AND ToolID = @ToolID
                            )
                            BEGIN
                                INSERT INTO profileTools (ProfileID, ToolID) 
                                VALUES (@ProfileID, @ToolID)
                            END";

                        using (var command = new SqlCommand(query, connection))
                        {

                            command.Parameters.AddWithValue("@ToolID", toolId);
                            command.Parameters.AddWithValue("@ProfileID", profileId);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddAssociationsByToolIdAsync(int toolId, List<string> profileList)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // move this foreach out for increased versatility
                    foreach (var name in profileList)
                    {
                        // check if association exists, then checks if profile exists
                        var queryCheckExistence = @"
                            DECLARE @ProfileID int
                            SET @ProfileID = (SELECT Id FROM profiles WHERE [Name] = @Name)

                            -- If profile exists, check if association does not exist
                            IF @ProfileID IS NOT NULL AND NOT EXISTS (
                                SELECT 1
                                FROM profileTools
                                WHERE ProfileID = @ProfileID
                                AND ToolID = @ToolID
                            )
                            BEGIN
                                INSERT INTO profileTools (ProfileID, ToolID) 
                                VALUES (@ProfileID, @ToolID)
                            END";

                        using (var commandCheckExistence = new SqlCommand(queryCheckExistence, connection))
                        {
                            commandCheckExistence.Parameters.AddWithValue("@Name", name);
                            commandCheckExistence.Parameters.AddWithValue("@ToolID", toolId);

                            await commandCheckExistence.ExecuteNonQueryAsync();
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteAssociationAsync(int toolId, string name) // change to delete all associations by tool
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"     
                        DECLARE @ProfileID int;
                        SET @ProfileID = (SELECT Id FROM profiles WHERE [Name] = @Name);

                        -- Delete from profileTools if the profile exists
                        IF @ProfileID IS NOT NULL
                        BEGIN
                            DELETE FROM [master].[dbo].[profileTools] 
                            WHERE ToolID = @ToolID
                            AND ProfileID = @ProfileID;
                        END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ToolID", toolId);
                        command.Parameters.AddWithValue("@Name", name);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<int> DeleteAllProfileAssociationsAsync(int profileId) // change to delete all associations by profile
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"DELETE FROM [master].[dbo].[profileTools] WHERE ProfileId = @ProfileId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Assuming there is an Id property for the WHERE clause
                        command.Parameters.AddWithValue("@ProfileId", profileId);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteAllToolAssociationsAsync(int toolId) // change to delete all associations by profile
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"DELETE FROM [master].[dbo].[profileTools] WHERE ToolID = @ToolID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Assuming there is an Id property for the WHERE clause
                        command.Parameters.AddWithValue("@ToolID", toolId);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private APIProfileDTO MapProfileFromReader(SqlDataReader reader)
        {
            var entity = new APIProfileDTO();
            foreach (var property in typeof(APIProfileDTO).GetProperties())
            {
                var columnName = property.Name;
                var value = reader[columnName];
                if (value != DBNull.Value)
                {
                    property.SetValue(entity, value);
                }
            }
            return entity;
        }

        private ProfileToolDTO MapAssociationsFromReader(SqlDataReader reader)
        {
            var entity = new ProfileToolDTO();
            foreach (var property in typeof(ProfileToolDTO).GetProperties())
            {
                var columnName = property.Name;
                var value = reader[columnName];
                if (value != DBNull.Value)
                {
                    property.SetValue(entity, value);
                }
            }
            return entity;
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