﻿using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common.Config;
using Microsoft.AspNetCore.Authorization;
using IntelligenceHub.Common;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminPolicy")]
    public class ToolController : ControllerBase
    {
        private readonly ProfileLogic _profileLogic;

        public ToolController(Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileLogic = new ProfileLogic(settings.DbConnectionString);
        }

        [HttpGet]
        [Route("get/{name}")]
        [ProducesResponseType(typeof(Tool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTool([FromRoute] string name) // get this to work with either a string or an int
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                var tool = await _profileLogic.GetTool(name);
                if (tool == null) return NotFound($"No tool with the name {name} exists");
                else return Ok(tool);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpGet]
        [Route("get/all")]
        [ProducesResponseType(typeof(IEnumerable<Tool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllTools()
        {
            try
            {
                var tools = await _profileLogic.GetAllTools();
                if (tools == null || tools.Count() < 1) return NotFound($"No tools exist. Make a post request to add some.");
                else return Ok(tools);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpGet]
        [Route("get/{name}/profiles")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetToolProfiles(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                var tool = await _profileLogic.GetToolProfileAssociations(name);
                if (tool == null) return NotFound($"The tool '{name}' is not associated with any profiles, or does not exist.");
                else return Ok(tool);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpPost]
        [Route("upsert")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddOrUpdateTool([FromBody] List<Tool> toolList)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateTools(toolList);
                if (errorMessage != null) return BadRequest(errorMessage);
                else return NoContent();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpPost]
        [Route("associate/{name}")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpPost]
        [Route("dissociate/{name}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpDelete]
        [Route("delete/{name}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTool([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                var success = await _profileLogic.DeleteTool(name);
                if (success) return NoContent();
                else return NotFound($"No tool with the name {name} exists");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}