using IntelligenceHub.Common.Attributes;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace IntelligenceHub.DAL
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class, new()
    {
        protected readonly string _connectionString;
        protected string _table;

        public GenericRepository(string connectionString)
        {
            _connectionString = connectionString;
            _table = GetTableName<T>();
        }

        // RAG databases names should be assigned via API request
        public GenericRepository(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _table = tableName;
        }

        public async Task<T> GetByNameAsync(string name)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"SELECT * FROM {_table} WHERE Name = @Name";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            if (await reader.ReadAsync())
                            {
                                return MapFromReader<T>(reader);
                            }
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
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            // add pagination
            try
            {
                var result = new List<T>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $"SELECT * FROM {_table}";
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapFromReader<T>(reader));
                    }
                }
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var properties = typeof(T).GetProperties().Where(p => !p.Name.Equals("Id") && p.Name != "Messages" && p.Name != "Tools" && p.Name != "Stop").ToList();
                    var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
                    var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                    var query = $@"     INSERT INTO {_table} ({columns})
                                        OUTPUT inserted.*
                                        VALUES ({values})";

                    using (var command = new SqlCommand(query, connection))
                    {
                        foreach (var property in typeof(T).GetProperties())
                        {
                            if (property.Name != "Messages" && property.Name != "Tools" && property.Name != "Stop")
                            {
                                var paramName = $"@{property.Name}";
                                var value = property.GetValue(entity) ?? DBNull.Value;
                                command.Parameters.AddWithValue(paramName, value);
                            }
                        }
                        using (var reader = await command.ExecuteReaderAsync())
                        if (await reader.ReadAsync())
                        {
                            return MapFromReader<T>(reader);
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

        public async Task<int> UpdateAsync(T existingEntity, T entity)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var setClause = string.Join(", ", typeof(T).GetProperties()
                        .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "Messages" && p.Name != "Tools") // check if all these are still needed
                        .Select(p => $"[{p.Name}] = @{p.Name}"));

                    var query = $@"
                        UPDATE {_table} SET {setClause} 
                        WHERE Name = @Name";

                    using (var command = new SqlCommand(query, connection))
                    {
                        foreach (var property in typeof(T).GetProperties())
                        {
                            if (property.Name != "Id" && property.Name != "Name" && property.Name != "Messages" && property.Name != "Tools")// check if all these are still needed
                            {
                                var paramName = $"@{property.Name}";
                                var value = property.GetValue(entity) ?? DBNull.Value;
                                command.Parameters.AddWithValue(paramName, value);
                            }
                        }
                        command.Parameters.AddWithValue("@Name", typeof(T).GetProperty("Name").GetValue(entity));
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeleteAsync(T entity)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"DELETE FROM {_table} WHERE Id = @Id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", typeof(T).GetProperty("Id").GetValue(entity));
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // declared as method in case reflection is desired for derived classes
        protected string GetTableName<T>()
        {
            var tableAttribute = typeof(T).GetCustomAttribute<TableNameAttribute>();
            return tableAttribute != null ? tableAttribute.TableName : string.Empty;
        }

        protected T MapFromReader<T>(SqlDataReader reader) where T : new()
        {
            var entity = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                var columnName = property.Name;
                if (columnName != "Messages" && columnName != "Tools")// check if all these are still needed
                {
                    var value = reader[columnName];
                    if (value != DBNull.Value)
                    {
                        // better way to do this?
                        if (property.PropertyType == typeof(float?) || property.PropertyType == typeof(float))
                        {
                            // Explicitly convert SQL float to C# float
                            var floatValue = Convert.ToSingle(value);
                            property.SetValue(entity, floatValue);
                        }
                        else
                        {
                            property.SetValue(entity, value);
                        }
                    }
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