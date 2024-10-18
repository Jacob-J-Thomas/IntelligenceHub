using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using System.Text;
using IntelligenceHub.Common.Handlers;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Exceptions;
using Newtonsoft.Json;
using IntelligenceHub.Common.Config;
using Microsoft.AspNetCore.Authorization;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class CompletionController : ControllerBase
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly ProfileValidationHandler _validationLogic;

        public CompletionController(ICompletionLogic completionLogic, Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _completionLogic = completionLogic;
            _validationLogic = new ProfileValidationHandler();
        }

        [HttpPost]
        [Route("Chat/{name}")]
        public async Task<IActionResult> CompletionStandard([FromRoute] string? name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                completionRequest.ProfileOptions.Name = name ?? completionRequest.ProfileOptions.Name;
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = await _completionLogic.ProcessCompletion(completionRequest);
                if (response is not null) return Ok(response);
                else return BadRequest("Invalid request. Please check your request body.");
            }
            catch(IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
            }
        }

        [HttpPost]
        [Route("SSE/{name}")]
        public async Task<IActionResult> CompletionStreaming([FromRoute] string? name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                completionRequest.ProfileOptions.Name = name ?? completionRequest.ProfileOptions.Name;
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = _completionLogic.StreamCompletion(completionRequest);

                // set headers to return SSE
                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                await foreach (var chunk in response)
                {
                    var jsonChunk = JsonConvert.SerializeObject(chunk);
                    var sseMessage = $"data: {jsonChunk}\n\n";
                    var data = Encoding.UTF8.GetBytes(sseMessage);
                    await Response.Body.WriteAsync(data, 0, data.Length);
                    await Response.Body.FlushAsync();
                }
                return new EmptyResult();
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
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
            }
        }
    }
}
