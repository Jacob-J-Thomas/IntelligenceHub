using IntelligenceHub.API.DTOs;
using IntelligenceHub.Controllers;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using IntelligenceHub.DAL.Tenant;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class TestTenantController : TenantControllerBase
    {
        public TestTenantController(IUserLogic userLogic, ITenantProvider tenantProvider) : base(userLogic, tenantProvider) {}
        public Task<APIResponseWrapper<Guid>> Invoke() => SetUserTenantContextAsync();
    }

    public class TenantControllerBaseTests
    {
        private readonly Mock<IUserLogic> _userLogic = new();
        private readonly Mock<ITenantProvider> _tenantProvider = new();
        private readonly TestTenantController _controller;

        public TenantControllerBaseTests()
        {
            _controller = new TestTenantController(_userLogic.Object, _tenantProvider.Object);
        }

        [Fact]
        public async Task SetUserTenantContextAsync_ReturnsFailure_WhenKeyMissing()
        {
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var result = await _controller.Invoke();

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.InternalError, result.StatusCode);
        }

        [Fact]
        public async Task SetUserTenantContextAsync_ReturnsFailure_WhenUserNotFound()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers["X-Api-Key"] = "key";
            _controller.ControllerContext.HttpContext = ctx;
            _userLogic.Setup(u => u.GetUserByApiTokenAsync("key")).ReturnsAsync((DbUser?)null);

            var result = await _controller.Invoke();

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.InternalError, result.StatusCode);
        }

        [Fact]
        public async Task SetUserTenantContextAsync_ReturnsSuccess_WhenUserExists()
        {
            var user = new DbUser { TenantId = Guid.NewGuid(), Sub = "sub" };
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers["X-Api-Key"] = "key";
            _controller.ControllerContext.HttpContext = ctx;
            _userLogic.Setup(u => u.GetUserByApiTokenAsync("key")).ReturnsAsync(user);

            var result = await _controller.Invoke();

            Assert.True(result.IsSuccess);
            Assert.Equal(user.TenantId, result.Data);
            _tenantProvider.VerifySet(t => t.TenantId = user.TenantId, Times.Once);
            _tenantProvider.VerifySet(t => t.User = user, Times.Once);
        }
    }
}
