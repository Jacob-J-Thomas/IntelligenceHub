﻿using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.API.MigratedDTOs;

namespace IntelligenceHub.Business
{
    public interface ICompletionLogic
    {
        IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest);
        Task<CompletionResponse> ProcessCompletion(CompletionRequest completionRequest);
        Task<List<HttpResponseMessage>> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, bool streaming = false);
    }
}