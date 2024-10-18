using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Exceptions;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ToolController : ControllerBase
    {
        //private readonly IConfiguration _configuration;
        private readonly ProfileLogic _profileLogic;

        public ToolController(Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileLogic = new ProfileLogic(settings.DbConnectionString);
        }

        [HttpGet]
        [Route("get/{name}")]
        public async Task<IActionResult> GetTool([FromRoute] string name) // get this to work with either a string or an int
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                var tool = await _profileLogic.GetTool(name);
                if (tool == null) return NotFound($"No tool with the name {name} exists");
                else return Ok(tool);
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

        [HttpGet]
        [Route("get/all")]
        public async Task<IActionResult> GetAllTools()
        {
            try
            {
                var tools = await _profileLogic.GetAllTools();
                if (tools == null || tools.Count() < 1) return NotFound($"No tools exist. Make a post request to add some.");
                else return Ok(tools);
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

        [HttpGet]
        [Route("get/{name}/profiles")]
        public async Task<IActionResult> GetToolProfiles(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                var tool = await _profileLogic.GetToolProfileAssociations(name);
                if (tool == null) return NotFound($"The tool '{name}' is not associated with any profiles, or does not exist.");
                else return Ok(tool);
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

        [HttpPost]
        [Route("upsert")]
        public async Task<IActionResult> AddOrUpdateTool([FromBody] List<Tool> toolList)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateTools(toolList);
                if (errorMessage != null) return BadRequest(errorMessage);
                else return NoContent();
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

        [HttpPost]
        [Route("associate/{name}")]
        public async Task<IActionResult> AddToolToProfiles([FromRoute] string name, List<string> profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                if (profiles == null || profiles.Count < 1) return BadRequest($"Invalid request.'Profiles' property cannot be null or empty: {profiles}.");
                var errorMessage = await _profileLogic.AddToolToProfiles(name, profiles);
                if (errorMessage == null) return Ok(await _profileLogic.GetToolProfileAssociations(name));
                else return NotFound(errorMessage);
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

        [HttpPost]
        [Route("dissociate/{name}")]
        public async Task<IActionResult> RemoveToolFromProfiles([FromRoute] string name, List<string> profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                if (profiles == null || profiles.Count < 1) return BadRequest($"Invalid request.'Profiles' property cannot be null or empty: {profiles}.");
                var errorMessage = await _profileLogic.DeleteToolAssociations(name, profiles);
                if (errorMessage == null) return NoContent();
                else return NotFound(errorMessage);
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

        [HttpDelete]
        [Route("delete/{name}")]
        public async Task<IActionResult> DeleteTool([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                var success = await _profileLogic.DeleteTool(name);
                if (success) return NoContent();
                else return NotFound($"No tool with the name {name} exists");
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
