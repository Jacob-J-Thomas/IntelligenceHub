using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Models;
using System;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class AuthLogicTests
    {
        private readonly AuthLogic _authLogic;
        private readonly JwtService _jwtService;

        public AuthLogicTests()
        {
            var settings = new AuthSettings
            {
                Domain = "test",
                Audience = "aud",
                JwtSecret = "secret"
            };
            _jwtService = new JwtService(settings);
            _authLogic = new AuthLogic(_jwtService);
        }

        [Fact]
        public async Task GetDefaultAuthToken_ShouldReturnToken()
        {
            var user = new DbUser { Sub = "sub", TenantId = Guid.NewGuid(), ApiToken = "t" };
            var result = await _authLogic.GetDefaultAuthToken(user);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result?.AccessToken));
        }

        [Fact]
        public async Task GetAdminAuthToken_ShouldReturnToken()
        {
            var user = new DbUser { Sub = "sub", TenantId = Guid.NewGuid(), ApiToken = "t" };
            var result = await _authLogic.GetAdminAuthToken(user);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result?.AccessToken));
        }
    }
}
