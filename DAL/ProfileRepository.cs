﻿using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.API.MigratedDTOs.ToolDTOs;
using IntelligenceHub.DAL.DTOs;
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
                    Id = (int)reader["ToolId"],
                    Name = (string)reader["ToolName"],
                    Description = reader["ToolDescription"] as string,
                    Required = reader["Required"] as string,
                }
            };
            return tools;
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