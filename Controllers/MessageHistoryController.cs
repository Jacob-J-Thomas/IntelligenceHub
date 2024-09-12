using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Controllers.DTOs;
using IntelligenceHub.Host.Config;
using IntelligenceHub.Business;
using System.Runtime;
using Azure;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using IntelligenceHub.DAL;
using Nest;
using Azure.Core;
using Microsoft.AspNetCore.Routing;
using System.Reflection.Metadata;
using IntelligenceHub.Business.ProfileLogic;
using IntelligenceHub.API.DTOs.ClientDTOs.ToolDTOs;
using IntelligenceHub.API.DTOs.ClientDTOs.MessageDTOs;

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
        [Route("get/conversation/{id}/{count}")]
        public async Task<IActionResult> GetConversation([FromRoute] Guid id, [FromRoute] int count) // get this to work with either a string or an int
        {
            try
            {
                if (count < 1) return BadRequest("count must be greater than 1");
                var conversation = await _messageHistoryLogic.GetConversationHistory(id, count);
                if (conversation is null || conversation.Count < 1) return NotFound($"The conversation '{id}' does not exist or is empty...");
                else return Ok(conversation);
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
        [Route("upsert/conversation")]
        public async Task<IActionResult> UpsertConversation([FromBody] List<DbMessageDTO> conversation)
        {
            try
            {
                var response = await _messageHistoryLogic.UpsertConversation(conversation);
                if (response is null || response.Count < 1) return BadRequest("Something went wrong while adding some messages to the database... please check your response body");
                else return Ok(response);
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
        [Route("add/message")]
        public async Task<IActionResult> AddMessage([FromBody] DbMessageDTO messageDTO)
        {
            try
            {
                if (messageDTO.ConversationId is null) return BadRequest("The ConversationId field is missing or invalid.");
                var response = await _messageHistoryLogic.AddMessage(messageDTO);
                if (response is not null) return Ok(response);
                else return BadRequest($"A conversation with the ID '{messageDTO.ConversationId}' does not exist.");
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
