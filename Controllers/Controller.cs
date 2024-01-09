using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;

namespace OpenAICustomFunctionCallingAPI.Controllers
{
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly Settings _settings;

        public Controller(Settings settings)
        {
            _settings = settings;
        }

        [HttpPost]
        [Route("/input")]
        public async Task<string> InputRouting([FromBody] InboundRequest body)
        {
            BusinessLogic business = new BusinessLogic(_settings);
            var completion = await business.BuildRoutingRequest(body);
            return completion;
        }
    }
}