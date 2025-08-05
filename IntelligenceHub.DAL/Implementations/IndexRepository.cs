using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using IntelligenceHub.DAL.Tenant;
using System.Text;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing index documents in the database. This repository is unique in that its table name is dynamic, and must be provided for each request.
    /// </summary>
    public class IndexRepository : GenericRepository<DbIndexDocument>, IIndexRepository
    {
        /// <summary>
        /// Constructor for the IndexRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        private readonly ITenantProvider _tenantProvider;
        public IndexRepository(IntelligenceHubDbContext context, ITenantProvider tenantProvider) : base(context, tenantProvider)
        {
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Sanitizes and normalizes any dynamic table name to ensure it is valid in SQL Server.
        /// </summary>
        private static string ToValidSqlTableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Table name cannot be null or empty.");

            var valid = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                    valid.Append(c);
                // skips dashes, underscores, spaces, and other non-alphanumerics
            }

            if (valid.Length == 0 || !char.IsLetter(valid[0]))
                valid.Insert(0, "TBL_");

            // Capitalize first letter for convention
            if (char.IsLower(valid[0]))
                valid[0] = char.ToUpper(valid[0]);

            if (valid.Length > 128)
                return valid.ToString().Substring(0, 128);

            return valid.ToString();
        }

        public async Task<int> GetRagIndexLengthAsync(string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var query = $@"SELECT COUNT(*) FROM [{tableName}]";
            return await _context.Database.ExecuteSqlRawAsync(query);
        }

        /// <summary>
        /// Retrieves a document from the RAG index by its title.
        /// </summary>
        /// <param name="tableName">The name of the RAG index.</param>
        /// <param name="title">The name of the document.</param>
        /// <returns>The document, or null if no matching entry is found.</returns>
        public async Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title)
        {
            tableName = ToValidSqlTableName(tableName);
            var query = $@"SELECT * FROM [{tableName}] WHERE Title = @Title";
            var parameters = new[] { new SqlParameter("@Title", title) };
            return await _dbSet.FromSqlRaw(query, parameters).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new table for RAG indexing.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success or failure of the operation.</returns>
        public async Task<bool> CreateIndexAsync(string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var contentTableQuery = $@"CREATE TABLE [{tableName}] (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                Title NVARCHAR(255) NOT NULL,
                                Content NVARCHAR(MAX) NOT NULL,
                                Topic NVARCHAR(255),
                                Keywords NVARCHAR(255),
                                Source NVARCHAR(4000) NOT NULL,
                                Created DATETIMEOFFSET NOT NULL,
                                Modified DATETIMEOFFSET NOT NULL
                            );";

            var tombstoneTableName = ToValidSqlTableName($"{tableName}_Deleted");
            var tombstoneTableQuery = $@"CREATE TABLE [{tombstoneTableName}] (
                Id INT PRIMARY KEY,
                DeletedAt DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME()
            );";

            await _context.Database.ExecuteSqlRawAsync(contentTableQuery);
            await _context.Database.ExecuteSqlRawAsync(tombstoneTableQuery);
            return true;
        }

        /// <summary>
        /// Enables change tracking for the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> EnableChangeTrackingAsync(string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var tombstoneTableName = ToValidSqlTableName($"{tableName}_Deleted");

            var contentTrackingQuery = $@"ALTER TABLE [dbo].[{tableName}]
                                  ENABLE CHANGE_TRACKING 
                                  WITH (TRACK_COLUMNS_UPDATED = ON);";

            var tombstoneTrackingQuery = $@"ALTER TABLE [dbo].[{tombstoneTableName}]
                                    ENABLE CHANGE_TRACKING 
                                    WITH (TRACK_COLUMNS_UPDATED = OFF);";

            await _context.Database.ExecuteSqlRawAsync(contentTrackingQuery);
            await _context.Database.ExecuteSqlRawAsync(tombstoneTrackingQuery);
            return true;
        }

        /// <summary>
        /// Creates or updates the SQL view that merges the main table and its tombstone table,
        /// exposing an IsDeleted flag for Azure Search’s soft‐delete policy.
        /// </summary>
        /// <param name="tableName">Base name of the index/content table (without “vw_” prefix or “_Deleted” suffix).</param>
        public async Task<bool> CreateDatasourceViewAsync(string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var tombstoneTableName = ToValidSqlTableName($"{tableName}_Deleted");
            string viewName = $"vw_{tableName}";
            string sql = $@"
                CREATE OR ALTER VIEW [dbo].[{viewName}] AS
                SELECT
                    Id,
                    Title,
                    Content,
                    Topic,
                    Keywords,
                    Source,
                    Created,
                    Modified,
                    CAST(0 AS BIT) AS IsDeleted
                FROM [dbo].[{tableName}]

                UNION ALL

                SELECT
                    Id,
                    NULL       AS Title,
                    NULL       AS Content,
                    NULL       AS Topic,
                    NULL       AS Keywords,
                    NULL       AS Source,
                    DeletedAt  AS Created,
                    DeletedAt  AS Modified,
                    CAST(1 AS BIT) AS IsDeleted
                FROM [dbo].[{tombstoneTableName}];
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);
            return true;
        }


        /// <summary>
        /// Marks the index to be updated by an indexer by setting its state to modified.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> MarkIndexForUpdateAsync(string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var query = $@"UPDATE [{tableName}] SET Modified = Modified";
            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        /// <summary>
        /// Deletes the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteIndexAsync(string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var tombstoneTableName = ToValidSqlTableName($"{tableName}_Deleted");

            var dropMainTableQuery = $@"DROP TABLE IF EXISTS [{tableName}];";
            var dropTombstoneQuery = $@"DROP TABLE IF EXISTS [{tombstoneTableName}];";

            await _context.Database.ExecuteSqlRawAsync(dropMainTableQuery);
            await _context.Database.ExecuteSqlRawAsync(dropTombstoneQuery);

            return true;
        }


        /// <summary>
        /// Retrieves all documents from the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <param name="count">The number of documents to retrieve.</param>
        /// <param name="page">The page number to offset the results by.</param>
        /// <returns>An IEnumerable containing the documents.</returns>
        public async Task<IEnumerable<DbIndexDocument>> GetAllAsync(string tableName, int count, int page)
        {
            tableName = ToValidSqlTableName(tableName);

            var skip = (page - 1) * count;

            var query = $@"SELECT * FROM [{tableName}]
                           ORDER BY Id
                           OFFSET {skip} ROWS
                           FETCH NEXT {count} ROWS ONLY";

            var responseCollection = await _dbSet.FromSqlRaw(query).ToListAsync();
            return responseCollection;
        }

        /// <summary>
        /// Adds a new document to the RAG index.
        /// </summary>
        /// <param name="document">The document being added.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>The newly added document.</returns>
        public async Task<DbIndexDocument> AddAsync(DbIndexDocument document, string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
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

        /// <summary>
        /// Updates an existing document in the RAG index.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <param name="document">The new definition of the document.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> UpdateAsync(int id, DbIndexDocument document, string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
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
                new SqlParameter("@Id", id),
                new SqlParameter("@Title", document.Title),
                new SqlParameter("@Content", document.Content),
                new SqlParameter("@Topic", document.Topic ?? (object)DBNull.Value),
                new SqlParameter("@Keywords", document.Keywords ?? (object)DBNull.Value),
                new SqlParameter("@Source", document.Source),
                new SqlParameter("@Created", document.Created),
                new SqlParameter("@Modified", document.Modified)
            };
            return await _context.Database.ExecuteSqlRawAsync(query, parameters) == 1;
        }

        /// <summary>
        /// Deletes a document from the RAG index.
        /// </summary>
        /// <param name="document">The document to be deleted.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A bool indicating the success or failure of the operation.</returns>
        public async Task<bool> DeleteAsync(DbIndexDocument document, string tableName)
        {
            tableName = ToValidSqlTableName(tableName);
            var tombstoneTableName = ToValidSqlTableName($"{tableName}_Deleted");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var insertTombstone = $@"
                INSERT INTO [{tombstoneTableName}] (Id)
                VALUES (@Id);
            ";

            var deleteContent = $@"
                DELETE FROM [{tableName}]
                WHERE Id = @Id;
            ";

            var idParam = new SqlParameter("@Id", document.Id);

            var tombstoneRows = await _context.Database.ExecuteSqlRawAsync(insertTombstone, idParam);
            var contentRows = await _context.Database.ExecuteSqlRawAsync(deleteContent, idParam);

            if (tombstoneRows == 1 && contentRows == 1)
            {
                await tx.CommitAsync();
                return true;
            }
            else
            {
                await tx.RollbackAsync();
                return false;
            }
        }
    }
}