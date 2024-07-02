using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using Nest;
using Azure.AI.OpenAI;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using System.Text;
using Microsoft.AspNetCore.Http;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.AICompletionDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.CompletionDTOs;

namespace OpenAICustomFunctionCallingAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompletionController : ControllerBase
    {
        private readonly CompletionLogic _completionLogic;
        private readonly ValidationLogic _validationLogic;

        public CompletionController(Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _completionLogic = new CompletionLogic(settings);
            _validationLogic = new ValidationLogic();
        }

        [HttpPost]
        [Route("Chat/{name}")]
        public async Task<IActionResult> CompletionRouting([FromRoute] string name, [FromBody] ChatRequestDTO completionRequest)
        {
            try
            {
                completionRequest.ProfileName = name ?? completionRequest.ProfileName;
                var errorMessage = _validationLogic.ValidateChatRequest(name, completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = await _completionLogic.ProcessCompletion(completionRequest);
                if (response is not null) return Ok(response);
                else return BadRequest("Invalid request. Please check your request body.");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("Chat/ClientBased/{name}")]
        public async Task<IActionResult> CompletionRouting([FromRoute] string name, [FromBody] ClientBasedCompletion completionRequest)
        {
            try
            {
                completionRequest.ProfileName = name ?? completionRequest.ProfileName;
                var errorMessage = _validationLogic.ValidateBaseDTO(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = await _completionLogic.ProcessClientBasedCompletion(completionRequest);
                if (response is not null) return Ok(response);
                else return BadRequest("Invalid request. Please check your request body.");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("Chat/Stream/{name}")]
        public async Task<IActionResult> CompletionStreaming([FromRoute] string name, [FromBody] ChatRequestDTO completionRequest)
        {
            try
            {
                var errorMessage = _validationLogic.ValidateChatRequest(name, completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                completionRequest.ProfileName = name ?? completionRequest.ProfileName;
                var response = await _completionLogic.StreamCompletion(completionRequest);
                if (response is not null)
                {
                    string author = null;
                    ResponseToolDTO tool = null;
                    Response.Headers.Add("Content-Type", "text/event-stream");
                    Response.Headers.Add("Cache-Control", "no-cache");
                    Response.Headers.Add("Connection", "keep-alive");
                    await foreach (var chunk in response)
                    {
                        var completionUpdate = chunk.ContentUpdate;
                        if (chunk.ToolCallUpdate is StreamingFunctionToolCallUpdate toolCall)
                        {
                            if (tool is null)
                            {
                                tool = new ResponseToolDTO();
                                tool.BuildFromStream(toolCall);
                                author = tool.Function.Name;

                            }
                            if (toolCall.ArgumentsUpdate != null)
                            {
                                tool.Function.Arguments += toolCall.ArgumentsUpdate;
                                completionUpdate = toolCall.ArgumentsUpdate;
                            }
                        }
                        if (author is null)
                        {
                            author = _completionLogic.GetStreamAuthor(chunk, completionRequest.ProfileName, completionRequest.ProfileModifiers.User);
                            author = chunk.AuthorName ?? author; // chunk.AuthorName can supposedly be assigned to via instructions in the system prompt
                        }
                        var sseMessage = $"data: {author}, {completionUpdate}\n\n";
                        var data = Encoding.UTF8.GetBytes(sseMessage);
                        await Response.Body.WriteAsync(data, 0, data.Length);
                        await Response.Body.FlushAsync();
                    }

                    // if tools were in the completion, execute them
                    if (tool is not null)
                    {
                        var toolList = new List<ResponseToolDTO>();
                        toolList.Add(tool);
                        var functionResponse = await _completionLogic.ExecuteTools(completionRequest.ConversationId, toolList, streaming: true);
                        var sseMessage = $"data: {tool.Function.Name}, {functionResponse}\n\n";
                        var data = Encoding.UTF8.GetBytes(sseMessage);
                        await Response.Body.WriteAsync(data, 0, data.Length);
                        await Response.Body.FlushAsync();
                    }
                    return new EmptyResult();
                }
                return BadRequest("Invalid request. Please check your request body.");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Chat/Stream/ClientBased/{name}")]
        public async Task<IActionResult> CompletionStreaming([FromRoute] string name, [FromBody] ClientBasedCompletion completionRequest)
        {
            try
            {
                completionRequest.ProfileName = name ?? completionRequest.ProfileName;
                var errorMessage = _validationLogic.ValidateBaseDTO(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = await _completionLogic.StreamClientBasedCompletion(completionRequest);
                if (response is not null)
                {
                    string author = null;
                    ResponseToolDTO tool = null;
                    Response.Headers.Add("Content-Type", "text/event-stream");
                    Response.Headers.Add("Cache-Control", "no-cache");
                    Response.Headers.Add("Connection", "keep-alive");
                    await foreach (var chunk in response)
                    {
                        var completionUpdate = chunk.ContentUpdate;
                        if (chunk.ToolCallUpdate is StreamingFunctionToolCallUpdate toolCall)
                        {
                            if (tool is null)
                            {
                                tool = new ResponseToolDTO();
                                tool.BuildFromStream(toolCall);
                                author = tool.Function.Name;

                            }
                            if (toolCall.ArgumentsUpdate is not null)
                            {
                                tool.Function.Arguments += toolCall.ArgumentsUpdate;
                                completionUpdate = toolCall.ArgumentsUpdate;
                            }
                        }
                        if (author is null)
                        {
                            author = _completionLogic.GetStreamAuthor(chunk, completionRequest.ProfileName, completionRequest.User);
                            author = chunk.AuthorName ?? author; // chunk.AuthorName can supposedly be assigned to via instructions in the system prompt
                        }
                        var sseMessage = $"data: {author}, {completionUpdate}\n\n";
                        var data = Encoding.UTF8.GetBytes(sseMessage);
                        await Response.Body.WriteAsync(data, 0, data.Length);
                        await Response.Body.FlushAsync();
                    }

                    // if tools were in the completion, execute them
                    if (tool is not null)
                    {
                        var toolList = new List<ResponseToolDTO>();
                        toolList.Add(tool);
                        var functionResponse = await _completionLogic.ExecuteTools(conversationId: null, toolList, streaming: true);
                        var sseMessage = $"data: {tool.Function.Name}, {functionResponse}\n\n";
                        var data = Encoding.UTF8.GetBytes(sseMessage);
                        await Response.Body.WriteAsync(data, 0, data.Length);
                        await Response.Body.FlushAsync();
                    }
                    return new EmptyResult();
                }
                return BadRequest("Invalid request. Please check your request body.");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
