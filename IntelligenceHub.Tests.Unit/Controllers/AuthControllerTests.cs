using IntelligenceHub.API.Controllers;
using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthLogic> _authLogicMock;
        private readonly Mock<IUserLogic> _userLogicMock;
        private readonly AuthController _sut;
        private const string ValidKey = "valid-key";
        private const string InvalidKey = "invalid-key";

        public AuthControllerTests()
        {
            _authLogicMock = new Mock<IAuthLogic>();
            _userLogicMock = new Mock<IUserLogic>();
            _sut = new AuthController(_authLogicMock.Object, _userLogicMock.Object);
        }

        // ────────────────────────────  ADMIN TOKEN  ────────────────────────────

        [Fact]
        public async Task GetAdminToken_ReturnsOk_WhenApiKeyValid()
        {
            // Arrange
            var token = new Auth0Response { AccessToken = "admin", ExpiresIn = 3600, TokenType = "Bearer" };
            var user = new DbUser();
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(ValidKey)).ReturnsAsync(user);
            _authLogicMock.Setup(x => x.GetAdminAuthToken(user)).ReturnsAsync(token);

            // Act
            var result = await _sut.GetAdminToken(ValidKey);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            Assert.Equal(token, ok.Value);
        }

        [Fact]
        public async Task GetAdminToken_ReturnsUnauthorized_WhenApiKeyInvalid()
        {
            // Arrange
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(InvalidKey)).ReturnsAsync((DbUser)null);

            // Act
            var result = await _sut.GetAdminToken(InvalidKey);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetAdminToken_ReturnsUnauthorized_WhenTokenNull()
        {
            // Arrange
            var user = new DbUser();
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(ValidKey)).ReturnsAsync(user);
            _authLogicMock.Setup(x => x.GetAdminAuthToken(user)).ReturnsAsync((Auth0Response)null);

            // Act
            var result = await _sut.GetAdminToken(ValidKey);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetAdminToken_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var user = new DbUser();
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(ValidKey)).ReturnsAsync(user);
            _authLogicMock.Setup(x => x.GetAdminAuthToken(user)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _sut.GetAdminToken(ValidKey);

            // Assert
            var err = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, err.StatusCode);
            Assert.Equal(GlobalVariables.DefaultExceptionMessage, err.Value);
        }

        // ────────────────────────────  DEFAULT TOKEN  ────────────────────────────

        [Fact]
        public async Task GetDefaultToken_ReturnsOk_WhenApiKeyValid()
        {
            // Arrange
            var token = new Auth0Response { AccessToken = "default", ExpiresIn = 3600, TokenType = "Bearer" };
            var user = new DbUser();
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(ValidKey)).ReturnsAsync(user);
            _authLogicMock.Setup(x => x.GetDefaultAuthToken(user)).ReturnsAsync(token);

            // Act
            var result = await _sut.GetDefaultToken(ValidKey);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            Assert.Equal(token, ok.Value);
        }

        [Fact]
        public async Task GetDefaultToken_ReturnsUnauthorized_WhenApiKeyInvalid()
        {
            // Arrange
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(InvalidKey)).ReturnsAsync((DbUser)null);

            // Act
            var result = await _sut.GetDefaultToken(InvalidKey);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetDefaultToken_ReturnsUnauthorized_WhenTokenNull()
        {
            // Arrange
            var user = new DbUser();
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(ValidKey)).ReturnsAsync(user);
            _authLogicMock.Setup(x => x.GetDefaultAuthToken(user)).ReturnsAsync((Auth0Response)null);

            // Act
            var result = await _sut.GetDefaultToken(ValidKey);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetDefaultToken_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var user = new DbUser();
            _userLogicMock.Setup(x => x.GetUserByApiTokenAsync(ValidKey)).ReturnsAsync(user);
            _authLogicMock.Setup(x => x.GetDefaultAuthToken(user)).ThrowsAsync(new Exception());

            // Act
            var result = await _sut.GetDefaultToken(ValidKey);

            // Assert
            var err = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, err.StatusCode);
            Assert.Equal(GlobalVariables.DefaultExceptionMessage, err.Value);
        }
    }
}
