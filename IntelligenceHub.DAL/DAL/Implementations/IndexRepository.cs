﻿using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IntelligenceHub.DAL.Implementations
{
    public class IndexRepository : GenericRepository<DbIndexDocument>, IIndexRepository
    {
        public IndexRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<int> GetRagIndexLengthAsync(string tableName)
        {
            var query = $@"SELECT COUNT(*) FROM [{tableName}]";
            return await _context.Database.ExecuteSqlRawAsync(query);
        }

        public async Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title)
        {
            var query = $@"SELECT * FROM [{tableName}] WHERE Title = @Title";
            var parameters = new[] { new SqlParameter("@Title", title) };
            return await _dbSet.FromSqlRaw(query, parameters).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateIndexAsync(string tableName)
        {
            var query = $@"CREATE TABLE [{tableName}] (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                Title NVARCHAR(255) NOT NULL,
                                Content NVARCHAR(MAX) NOT NULL,
                                Topic NVARCHAR(255),
                                Keywords NVARCHAR(255),
                                Source NVARCHAR(510) NOT NULL,
                                Created DATETIMEOFFSET NOT NULL,
                                Modified DATETIMEOFFSET NOT NULL
                            );";

            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        public async Task<bool> EnableChangeTrackingAsync(string tableName)
        {
            var query = $@"ALTER TABLE [dbo].[{tableName}]
                           ENABLE CHANGE_TRACKING 
                           WITH (TRACK_COLUMNS_UPDATED = ON);";

            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        public async Task<bool> MarkIndexForUpdateAsync(string tableName)
        {
            // Dummy operation to mark the whole index as updated
            var query = $@"UPDATE [{tableName}] SET Modified = Modified";
            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        public async Task<bool> DeleteIndexAsync(string tableName)
        {
            var query = $@"DROP TABLE IF EXISTS [{tableName}]";
            var rows = await _context.Database.ExecuteSqlRawAsync(query);
            return rows > 0;
        }

        public async Task<IEnumerable<DbIndexDocument>> GetAllAsync(string tableName, int count, int page)
        {
            // Calculate the number of rows to skip based on the page number
            var skip = (page - 1) * count;

            // Formulate the SQL query for pagination
            var query = $@"SELECT * FROM [{tableName}]
                           ORDER BY Id
                           OFFSET {skip} ROWS
                           FETCH NEXT {count} ROWS ONLY";

            // Execute the query and return the result
            var responseCollection = await _dbSet.FromSqlRaw(query).ToListAsync();
            return responseCollection;
        }

        public async Task<DbIndexDocument> AddAsync(DbIndexDocument document, string tableName)
        {
            var query = $@"INSERT INTO [{tableName}] (Title, Content, Topic, Keywords, Source, Created, Modified)
                               VALUES (@Title, @Content, @Topic, @Keywords, @Source, @Created, @Modified);
                               SELECT CAST(SCOPE_IDENTITY() as int);";
            var parameters = new[]
            {
                    new SqlParameter("@Title", document.Title),
                    new SqlParameter("@Content", document.Content),
                    new SqlParameter("@Topic", document.Topic ?? (object)DBNull.Value),
                    new SqlParameter("@Keywords", document.Keywords ?? (object)DBNull.Value),
                    new SqlParameter("@Source", document.Source),
                    new SqlParameter("@Created", document.Created),
                    new SqlParameter("@Modified", document.Modified)
                };
            document.Id = await _context.Database.ExecuteSqlRawAsync(query, parameters);
            return document;
        }

        public async Task<int> UpdateAsync(DbIndexDocument existing, DbIndexDocument document, string tableName)
        {
            var query = $@"UPDATE [{tableName}] SET 
                               Title = @Title, 
                               Content = @Content, 
                               Topic = @Topic, 
                               Keywords = @Keywords, 
                               Source = @Source, 
                               Created = @Created, 
                               Modified = @Modified 
                               WHERE Id = @Id";
            var parameters = new[]
            {
                    new SqlParameter("@Id", existing.Id),
                    new SqlParameter("@Title", document.Title),
                    new SqlParameter("@Content", document.Content),
                    new SqlParameter("@Topic", document.Topic ?? (object)DBNull.Value),
                    new SqlParameter("@Keywords", document.Keywords ?? (object)DBNull.Value),
                    new SqlParameter("@Source", document.Source),
                    new SqlParameter("@Created", document.Created),
                    new SqlParameter("@Modified", document.Modified)
                };
            return await _context.Database.ExecuteSqlRawAsync(query, parameters);
        }

        public async Task<int> DeleteAsync(DbIndexDocument document, string tableName)
        {
            var query = $@"DELETE FROM [{tableName}] WHERE Id = @Id";
            var parameters = new[]
            {
                    new SqlParameter("@Id", document.Id)
                };
            return await _context.Database.ExecuteSqlRawAsync(query, parameters);
        }
    }
}