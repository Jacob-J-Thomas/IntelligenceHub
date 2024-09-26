using Microsoft.Identity.Client;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.DAL;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.Common.Exceptions;

namespace IntelligenceHub.Business
{
    public class MessageHistoryLogic
    {
        private readonly MessageHistoryRepository _messageHistoryRepository;

        public MessageHistoryLogic(string dbConnectionString)
        {
            _messageHistoryRepository = new MessageHistoryRepository(dbConnectionString);
        }

        public async Task<List<DbMessage>> GetConversationHistory(Guid id, int count)
        {
            return await _messageHistoryRepository.GetConversationAsync(id, count);
        }

        public async Task<List<DbMessage>> UpsertConversation(List<DbMessage> messages)
        {
            var addedMessages = new List<DbMessage>();
            foreach (var message in messages)
            {
                var response = await _messageHistoryRepository.AddAsync(message);
                if (response is null) return null;
                addedMessages.Add(response);
            }
            return addedMessages;
        }

        public async Task<bool> DeleteConversation(Guid id)
        {
            return await _messageHistoryRepository.DeleteConversationAsync(id);
        }

        public async Task<DbMessage> AddMessage(Guid conversationId, Message message)
        {
            var dbMessage = DbMappingHandler.MapToDbMessage(message);
            var conversation = await _messageHistoryRepository.GetConversationAsync(conversationId, 1);
            if (conversation == null) throw new IntelligenceHubException(404, $"A conversations with id '{conversationId}' does not exist."); 
            return await _messageHistoryRepository.AddAsync(dbMessage);
        }

        public async Task<bool> DeleteMessage(Guid conversationId, int messageId)
        {
            return await _messageHistoryRepository.DeleteMessageAsync(conversationId, messageId);
        }
    }
}
