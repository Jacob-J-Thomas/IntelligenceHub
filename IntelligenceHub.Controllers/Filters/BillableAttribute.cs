using IntelligenceHub.Business.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace IntelligenceHub.Controllers.Filters
{
    /// <summary>
    /// Action filter that records billing information for each request.
    /// </summary>
    public class BillableAttribute : ActionFilterAttribute
    {
        private readonly UsageType _usageType;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillableAttribute"/> class.
        /// </summary>
        /// <param name="usageType">The usage type for billing.</param>
        public BillableAttribute(UsageType usageType = UsageType.General)
        {
            _usageType = usageType;
        }

        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();
            if (executedContext.Exception != null) return;

            var billingService = context.HttpContext.RequestServices.GetService(typeof(IBillingService)) as IBillingService;
            if (billingService == null) return;

            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var subscriptionItemId = $"{userId}:{_usageType.ToString().ToLower()}";
            await billingService.TrackUsageAsync(subscriptionItemId, 1);
        }
    }

    /// <summary>
    /// Usage type enumeration for billing metrics.
    /// </summary>
    public enum UsageType
    {
        /// <summary>
        /// General API usage.
        /// </summary>
        General,
        /// <summary>
        /// Completion specific usage.
        /// </summary>
        Completion
    }
}
