using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;

namespace OpenAICustomFunctionCallingAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ResponseController : ControllerBase
    {
        private readonly Settings _settings;

        public ResponseController(Settings settings)
        {
            _settings = settings;
        }

        [HttpPost]
        [Route("Completion")]
        public async Task<string> CompletionRouting([FromBody] InboundRequest body)
        {
            BusinessLogic business = new BusinessLogic(_settings);
            var completion = await business.GetCompletion(body, true);
            return completion;
        }
    }
}
