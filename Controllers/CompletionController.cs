using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Host.Config;
using IntelligenceHub.Business;
using IntelligenceHub.API.DTOs.ClientDTOs.CompletionDTOs.Response;
using System.Text;
using IntelligenceHub.Common.Handlers;
using IntelligenceHub.API.MigratedDTOs;
using IntelligenceHub.Common.Exceptions;
using Newtonsoft.Json;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompletionController : ControllerBase
    {
        private readonly CompletionLogic _completionLogic;
        private readonly ProfileAndToolValidationHandler _validationLogic;

        public CompletionController(IHttpClientFactory clientFactory, Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _completionLogic = new CompletionLogic(clientFactory, settings);
            _validationLogic = new ProfileAndToolValidationHandler();
        }

        [HttpPost]
        [Route("Chat/{name}")]
        public async Task<IActionResult> CompletionRouting([FromRoute] string name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                completionRequest.ProfileOptions.Model = name ?? completionRequest.ProfileOptions.Model;
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
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Chat/Stream/{name}")]
        public async Task<IActionResult> CompletionStreaming([FromRoute] string name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                completionRequest.ProfileOptions.Model = name ?? completionRequest.ProfileOptions.Model;
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = _completionLogic.StreamCompletion(completionRequest);

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
            catch (Exception)
            {
                throw;
            }
        }
    }
}
