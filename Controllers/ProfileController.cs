using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        //private readonly IConfiguration _configuration;
        private ProfileLogic _profileLogic;

        public ProfileController(Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileLogic = new ProfileLogic(settings.DbConnectionString);
        }

        [HttpGet]
        [Route("get/{name}")]
        public async Task<IActionResult> GetProfile([FromRoute] string name) // get this to work with either a string or an int
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid route data. Please check your input.");
                var profileDto = await _profileLogic.GetProfile(name);
                if (profileDto is not null) return Ok(profileDto);
                return NotFound($"No profile with the name {name} was found.");
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
        public async Task<IActionResult> GetAllProfiles()
        {
            try
            {
                return Ok(await _profileLogic.GetAllProfiles());
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
        public async Task<IActionResult> AddOrUpdateProfile([FromBody] Profile profileDto)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateProfile(profileDto);
                if (errorMessage is not null) return BadRequest(errorMessage);
                else return Ok(await _profileLogic.GetProfile(profileDto.Name));
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
        public async Task<IActionResult> AddProfileToTools([FromRoute] string name, List<string> tools)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                if (tools is null || tools.Count < 1) return BadRequest($"Invalid request.'Profiles' property cannot be null or empty: {tools}.");
                var errorMessage = await _profileLogic.AddProfileToTools(name, tools);
                if (errorMessage is null) return Ok(await _profileLogic.GetProfileToolAssociations(name));
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
        public async Task<IActionResult> RemoveProfileFromTools([FromRoute] string name, List<string> tools)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                if (tools is null || tools.Count < 1) return BadRequest($"Invalid request.'Profiles' property cannot be null or empty: {tools}.");
                var errorMessage = await _profileLogic.DeleteProfileAssociations(name, tools);
                if (errorMessage is null) return NoContent();
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
        public async Task<IActionResult> DeleteProfile([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name: {name}.");
                var errorMessage = await _profileLogic.DeleteProfile(name);
                if (errorMessage is not null) return NotFound(errorMessage);
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
    }
}
