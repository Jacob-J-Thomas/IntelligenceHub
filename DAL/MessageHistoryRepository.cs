using Nest;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.ToolDTOs;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace OpenAICustomFunctionCallingAPI.DAL
{
    public class MessageHistoryRepository : GenericRepository<DbMessageDTO>
    {
        public MessageHistoryRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<List<DbMessageDTO>> GetConversationAsync(Guid conversationId, int maxMessages)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        SELECT TOP(@MaxMessages) *
                        FROM MessageHistory
                        WHERE [ConversationId] = @ConversationId
                        ORDER BY timestamp DESC;";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaxMessages", maxMessages);
                        command.Parameters.AddWithValue("@ConversationId", conversationId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var conversationHistory = new List<DbMessageDTO>();
                            while (await reader.ReadAsync())
                            {
                                var dbMessage = MapFromReader<DbMessageDTO>(reader);
                                conversationHistory.Add(dbMessage);
                            }
                            return conversationHistory;
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteConversationAsync(Guid conversationId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                        DELETE FROM MessageHistory 
                        WHERE [ConversationId] = @ConversationId;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ConversationId", conversationId);
                        var response = await command.ExecuteNonQueryAsync();
                        if (response > 0) return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteMessageAsync(Guid conversationId, int messageId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = $@"
                        DELETE FROM MessageHistory 
                        WHERE [ConversationId] = @ConversationId AND [Id] = @MessageId;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ConversationId", conversationId);
                        command.Parameters.AddWithValue(@"MessageId", messageId);
                        var response = await command.ExecuteNonQueryAsync();
                        if (response > 0) return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}