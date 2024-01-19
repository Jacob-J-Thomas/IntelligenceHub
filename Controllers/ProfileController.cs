using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;
using System.Runtime;

namespace OpenAICustomFunctionCallingAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly Settings _settings;

        public ProfileController(Settings settings)
        {
            _settings = settings;
        }

        [HttpGet]
        [Route("get")]
        public async Task<string> GetProfile([FromBody] InboundRequest body)
        {
            return "placeholder";
        }

        [HttpPost]
        [Route("add")]
        public async Task<string> CreateProfile([FromBody] InboundRequest body)
        {
            return "placeholder";
        }

        [HttpPut]
        [Route("modify")]
        public async Task<string> ModifyProfile([FromBody] InboundRequest body)
        {
            return "placeholder";
        }

        [HttpDelete]
        [Route("remove")]
        public async Task<string> DeleteProfile([FromBody] InboundRequest body)
        {
            return "placeholder";
        }
    }
}
