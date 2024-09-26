using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Host.Config;
using IntelligenceHub.Business;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.Common.Exceptions;

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
        [Route("get/conversation/{id}/count/{count}")]
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
        [Route("upsert/conversation/{id}")]
        public async Task<IActionResult> UpsertConversation([FromBody] List<DbMessage> conversation)
        {
            try
            {
                var response = await _messageHistoryLogic.UpsertConversation(conversation);
                if (response is null || response.Count < 1) return BadRequest("Something went wrong while adding some messages to the database... please check your response body");
                else return Ok(response);
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
        [Route("delete/conversation/{id}")]
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

        [HttpPost]
        [Route("conversation/{conversationId}/messages")]
        public async Task<IActionResult> AddMessage([FromRoute] Guid conversationId, [FromBody] Message messageDTO)
        {
            try
            {
                // below isn't required since Guid is non nullable, but double check via testing before deleting these lines
                //if (conversationId == null) return BadRequest("The ConversationId field is missing or invalid.");
                var response = await _messageHistoryLogic.AddMessage(messageDTO);
                if (response is not null) return Ok(response);
                else return BadRequest($"A conversation with the ID '{messageDTO.ConversationId}' does not exist.");
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
        [Route("delete/message/{conversationId}/{messageId}")]
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
