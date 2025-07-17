using System.Security.Claims;
using IntelligenceHub.Common.Interfaces;

namespace IntelligenceHub.Host.Middleware
{
    /// <summary>
    /// Middleware that populates the <see cref="IUserIdAccessor"/> with the authenticated user identifier.
    /// </summary>
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public UserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Sets the current user identifier on the <see cref="IUserIdAccessor"/> and continues the pipeline.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="accessor">Accessor used to store the user identifier.</param>
        /// <returns>An awaitable task.</returns>
        public async Task InvokeAsync(HttpContext context, IUserIdAccessor accessor)
        {
            accessor.UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _next(context);
        }
    }
}
