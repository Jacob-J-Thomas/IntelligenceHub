using Microsoft.Identity.Client;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.MessageDTOs;
using OpenAICustomFunctionCallingAPI.DAL;

namespace OpenAICustomFunctionCallingAPI.Business
{
    public class MessageHistoryLogic
    {
        private readonly MessageHistoryRepository _messageHistoryRepository;

        public MessageHistoryLogic(string dbConnectionString)
        {
            _messageHistoryRepository = new MessageHistoryRepository(dbConnectionString);
        }

        public async Task<List<DbMessageDTO>> GetConversationHistory(Guid id, int count)
        {
            return await _messageHistoryRepository.GetConversationAsync(id, count);
        }

        public async Task<List<DbMessageDTO>> UpsertConversation(List<DbMessageDTO> messages)
        {
            var addedMessages = new List<DbMessageDTO>();
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

        public async Task<DbMessageDTO> AddMessage(DbMessageDTO message)
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
