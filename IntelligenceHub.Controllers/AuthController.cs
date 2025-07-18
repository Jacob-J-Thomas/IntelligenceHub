using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace IntelligenceHub.API.Controllers
{
    /// <summary>
    /// Controller for handling authentication-related requests.
    /// </summary>
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthLogic _authLogic;
        private readonly IUserLogic _userLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authLogic">The authentication logic.</param>
        public AuthController(IAuthLogic authLogic, IUserLogic userLogic)
        {
            _authLogic = authLogic;
            _userLogic = userLogic;
        }

        /// <summary>
        /// Gets the admin authentication token.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the admin authentication token.</returns>
        [HttpGet("admintoken")]
        [AllowAnonymous]
        [SwaggerOperation(OperationId = "GetElevatedAuthToken")]
        [ProducesResponseType(typeof(Auth0Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdminToken([FromHeader(Name = "X-Api-Key")] string apiKey)
        {
            try
            {
                var user = await _userLogic.GetUserByApiTokenAsync(apiKey);
                if (user is null) return Unauthorized();

                var token = await _authLogic.GetAdminAuthToken();
                return token is null ? Unauthorized() : Ok(token);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// Gets the default authentication token.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the default authentication token.</returns>
        [HttpGet("defaulttoken")]
        [AllowAnonymous]
        [SwaggerOperation(OperationId = "GetDefaultAuthToken")]
        [ProducesResponseType(typeof(Auth0Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDefaultToken([FromHeader(Name = "X-Api-Key")] string apiKey)
        {
            try
            {
                var user = await _userLogic.GetUserByApiTokenAsync(apiKey);
                if (user is null) return Unauthorized();

                var token = await _authLogic.GetDefaultAuthToken();
                return token is null ? Unauthorized() : Ok(token);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
