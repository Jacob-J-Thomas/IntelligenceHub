using Microsoft.Identity.Client;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.DAL;

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

        public async Task<DbMessage> AddMessage(DbMessage message)
        {
            var conversation = await _messageHistoryRepository.GetConversationAsync((Guid)message.ConversationId, 1);
            if (conversation is null || conversation.Count < 1) return null; 
            return await _messageHistoryRepository.AddAsync(message);
        }

        public async Task<bool> DeleteMessage(Guid conversationId, int messageId)
        {
            return await _messageHistoryRepository.DeleteMessageAsync(conversationId, messageId);
        }
    }
}
