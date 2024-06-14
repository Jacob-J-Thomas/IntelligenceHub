using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;
using System.Runtime;
using Azure;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.DAL;
using Nest;
using OpenAICustomFunctionCallingAPI.Business.ProfileLogic;

namespace OpenAICustomFunctionCallingAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
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
        [Route("upsert")]
        public async Task<IActionResult> AddOrUpdateProfile([FromBody] APIProfileDTO profileDto)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateProfile(profileDto);
                if (errorMessage is not null) return BadRequest(errorMessage);
                else return Ok(await _profileLogic.GetProfile(profileDto.Name));
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
