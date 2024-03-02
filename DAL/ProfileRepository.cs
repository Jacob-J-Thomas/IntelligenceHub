using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public class ProfileRepository : GenericRepository<DbProfileDTO>
    {
        private readonly string _connectionString;

        public ProfileRepository(string connectionString) : base(connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<APIProfileDTO> GetByNameWithToolsAsync(string Name)
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
                            t.Type AS ToolType,
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
                                var profile = MapProfileFromReader(reader);
                                var toolList = MapToolsFromReader(reader);

                                profile.Tools = new List<Tool>();

                                // probably move this foreach into 
                                foreach (var tool in toolList)
                                {
                                    profile.Tools.Add(new Tool(tool));
                                }
                                return profile;
                            }
                        }
                    }
                    return null; /// Return null if no result is found
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private APIProfileDTO MapProfileFromReader(SqlDataReader reader)
        {
            var profile = new DbProfileDTO
            {
                Id = (int)reader["Id"],
                Name = (string)reader["Name"],
                Response_Format = reader["Response_Format"] as string,
                Logprobs = reader["Logprobs"] as bool?,
                Reference_Profiles = reader["Reference_Profiles"] as string,
                Stop_Sequences = reader["Stop_Sequences"] as string,
                System_Message = reader["System_Message"] as string,
                Model = reader["Model"] as string,
                Frequency_Penalty = reader["frequency_penalty"] as double?,
                Presence_Penalty = reader["presence_penalty"] as double?,
                Temperature = reader["Temperature"] as double?,
                Top_P = reader["top_p"] as double?,
                Stream = reader["Stream"] as bool?,
                Max_Tokens = reader["max_tokens"] as int?,
                N = reader["N"] as int?,
                Top_Logprobs = reader["top_logprobs"] as int?,
                Seed = reader["Seed"] as int?,
                User = reader["User"] as string,
                Tool_Choice = reader["tool_choice"] as string
            };
            return new APIProfileDTO(profile);
        }

        private List<DbToolDTO> MapToolsFromReader(SqlDataReader reader)
        {
            var tools = new List<DbToolDTO>
            {
                new DbToolDTO
                {
                    Id = (int)reader["ToolId"],
                    Name = (string)reader["ToolName"],
                    Description = reader["ToolDescription"] as string,
                    Type = (string)reader["ToolType"],
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