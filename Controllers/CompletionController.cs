using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using Nest;

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
                var errorMessage = _validationLogic.ValidateChatRequest(name, completionRequest);
                if (errorMessage != null)
                {
                    return BadRequest(errorMessage);
                }

                completionRequest.ProfileName = name ?? completionRequest.ProfileName;

                var response = await _completionLogic.ProcessCompletion(completionRequest);
                if (response != null)
                {
                    return Ok(response);
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
