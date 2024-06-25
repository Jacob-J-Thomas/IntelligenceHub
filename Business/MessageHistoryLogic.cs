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

        public async Task<bool> UpsertConversation(List<DbMessageDTO> messages)
        {
            foreach (var message in messages)
            {
                var response = await _messageHistoryRepository.AddAsync(message);
                if (response is null) return false;
            }
            return true;
        }

        public async Task<bool> DeleteConversation(Guid id)
        {
            return await _messageHistoryRepository.DeleteConversationAsync(id);
        }

        public async Task<bool> AddMessage(DbMessageDTO message)
        {
            var response = await _messageHistoryRepository.AddAsync(message);
            if (response is null) return false;
            else return true;
        }

        public async Task<bool> DeleteMessage(Guid conversationId, int messageId)
        {
            return await _messageHistoryRepository.DeleteMessageAsync(conversationId, messageId);
        }
    }
}
