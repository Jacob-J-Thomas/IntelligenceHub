using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MessageHistoryController : ControllerBase
    {
        //private readonly IConfiguration _configuration;
        private readonly MessageHistoryLogic _messageHistoryLogic;

        public MessageHistoryController(Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _messageHistoryLogic = new MessageHistoryLogic(settings.DbConnectionString);
        }

        [HttpGet]
        [Route("conversation/{id}/count/{count}")]
        public async Task<IActionResult> GetConversation([FromRoute] Guid id, [FromRoute] int count) // get this to work with either a string or an int
        {
            try
            {
                if (count < 1) return BadRequest("count must be greater than 1");
                var conversation = await _messageHistoryLogic.GetConversationHistory(id, count);
                if (conversation is null || conversation.Count < 1) return NotFound($"The conversation '{id}' does not exist or is empty...");
                else return Ok(conversation);
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
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
        [Route("conversation/{id}")]
        public async Task<IActionResult> UpsertConversation([FromRoute] Guid id,[FromBody] List<Message> messages)
        {
            try
            {
                var responseMessages = await _messageHistoryLogic.UpsertConversation(id, messages);
                if (responseMessages is null || responseMessages.Count != messages.Count) throw new IntelligenceHubException(500, $"Something went wrong when adding the messages. Only {responseMessages?.Count ?? 0} of {messages.Count} messages were added.");
                else return Ok(responseMessages);
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
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
        [Route("conversation/{id}")]
        public async Task<IActionResult> AddMessage([FromRoute] Guid id, [FromBody] Message message)
        {
            try
            {
                var response = await _messageHistoryLogic.AddMessage(id, message);
                if (response is not null) return Ok(response);
                else return BadRequest($"A conversation with the ID '{id}' does not exist.");
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
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

        [HttpDelete]
        [Route("conversation/{id}")]
        public async Task<IActionResult> DeleteConversation([FromRoute] Guid id)
        {
            try
            {
                var response = await _messageHistoryLogic.DeleteConversation(id);
                if (response) return Ok(response);
                else return NotFound($"No conversation with ID '{id}' was found");
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
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

        [HttpDelete]
        [Route("conversation/{conversationId}/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid conversationId, [FromRoute] int messageId)
        {
            try
            {
                var response = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);
                if (response) return Ok(response);
                else return NotFound("The conversation or message was not found");
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
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
