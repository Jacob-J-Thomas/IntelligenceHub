﻿using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;

namespace IntelligenceHub.DAL
{
    public class MessageHistoryRepository : GenericRepository<DbMessage>
    {
        public MessageHistoryRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<List<Message>> GetConversationAsync(Guid conversationId, int maxMessages)
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
                            var conversationHistory = new List<Message>();
                            while (await reader.ReadAsync())
                            {
                                var dbMessage = MapFromReader<DbMessage>(reader);
                                var mappedMessage = DbMappingHandler.MapFromDbMessage(dbMessage);
                                conversationHistory.Add(mappedMessage);
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