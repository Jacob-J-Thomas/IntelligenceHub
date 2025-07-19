using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Extensions.Options;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class ProfileLogicTests
    {
        private readonly Mock<IProfileRepository> _mockProfileRepository;
        private readonly Mock<IProfileToolsAssociativeRepository> _mockProfileToolsRepository;
        private readonly Mock<IToolRepository> _mockToolRepository;
        private readonly Mock<IPropertyRepository> _mockPropertyRepository;
        private readonly Mock<IValidationHandler> _mockValidationHandler;
        private readonly Mock<IOptionsMonitor<Settings>> _mockIOptions;
        private readonly ProfileLogic _profileLogic;

        public ProfileLogicTests()
        {
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockProfileToolsRepository = new Mock<IProfileToolsAssociativeRepository>();
            _mockToolRepository = new Mock<IToolRepository>();
            _mockPropertyRepository = new Mock<IPropertyRepository>();
            _mockValidationHandler = new Mock<IValidationHandler>();
            _mockIOptions = new Mock<IOptionsMonitor<Settings>>();

            var settings = new Settings { ValidAGIModels = new[] { "Model1", "Model2" } };
            _mockIOptions.Setup(m => m.CurrentValue).Returns(settings);

            _profileLogic = new ProfileLogic(
                _mockIOptions.Object,
                _mockProfileRepository.Object,
                _mockProfileToolsRepository.Object,
                _mockToolRepository.Object,
                _mockPropertyRepository.Object,
                _mockValidationHandler.Object
            );
        }

        [Fact]
        public async Task GetProfile_ReturnsFailure_WhenProfileDoesNotExist()
        {
            // Arrange
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DbProfile)null);

            // Act
            var result = await _profileLogic.GetProfile("NonExistentProfile");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal("No profile with the name 'NonExistentProfile' was found.", result.ErrorMessage);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetProfile_ReturnsProfile_WhenProfileExists()
        {
            // Arrange
            var profile = new DbProfile { Id = 1, Name = "ExistingProfile", Host = AGIServiceHost.Azure.ToString() };
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockProfileToolsRepository.Setup(repo => repo.GetToolAssociationsAsync(It.IsAny<int>())).ReturnsAsync(new List<DbProfileTool>());

            // Act
            var result = await _profileLogic.GetProfile("ExistingProfile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ExistingProfile", result.Data.Name);
        }

        [Fact]
        public async Task CreateOrUpdateProfile_ReturnsErrorMessage_WhenValidationFails()
        {
            // Arrange
            var profile = new Profile { Name = "InvalidProfile" };
            _mockValidationHandler.Setup(handler => handler.ValidateAPIProfile(It.IsAny<Profile>())).Returns("Validation Error");

            // Act
            var result = await _profileLogic.CreateOrUpdateProfile(profile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Validation Error", result.ErrorMessage);
            Assert.Equal(APIResponseStatusCodes.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateOrUpdateProfile_CreatesNewProfile_WhenProfileDoesNotExist()
        {
            // Arrange
            var profile = new Profile { Name = "NewProfile" };
            _mockValidationHandler.Setup(handler => handler.ValidateAPIProfile(It.IsAny<Profile>())).Returns((string)null);
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DbProfile)null);
            _mockProfileRepository.Setup(repo => repo.AddAsync(It.IsAny<DbProfile>())).ReturnsAsync(new DbProfile());

            // Act
            var result = await _profileLogic.CreateOrUpdateProfile(profile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Data);
            _mockProfileRepository.Verify(repo => repo.AddAsync(It.IsAny<DbProfile>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateProfile_UpdatesProfile_WhenProfileExists()
        {
            var profileDto = new Profile { Name = "ExistingProfile", Tools = new List<Tool>() };
            var existingDbProfile = new DbProfile { Id = 1, Name = "ExistingProfile", Host = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(h => h.ValidateAPIProfile(It.IsAny<Profile>())).Returns((string?)null);
            _mockProfileRepository.Setup(r => r.GetByNameAsync(profileDto.Name)).ReturnsAsync(existingDbProfile);
            _mockProfileRepository.Setup(r => r.UpdateAsync(existingDbProfile)).ReturnsAsync(existingDbProfile);
            _mockProfileToolsRepository.Setup(r => r.DeleteAllProfileAssociationsAsync(existingDbProfile.Id)).ReturnsAsync(true);

            var result = await _profileLogic.CreateOrUpdateProfile(profileDto);

            Assert.True(result.IsSuccess);
            _mockProfileRepository.Verify(r => r.UpdateAsync(existingDbProfile), Times.Once);
            _mockProfileToolsRepository.Verify(r => r.DeleteAllProfileAssociationsAsync(existingDbProfile.Id), Times.Once);
        }

        [Fact]
        public async Task AddProfileToTools_ReturnsFailure_WhenToolNotFound()
        {
            var tools = new List<string> { "Tool1" };
            _mockToolRepository.Setup(r => r.GetByNameAsync("Tool1")).ReturnsAsync((DbTool?)null);

            var result = await _profileLogic.AddProfileToTools("Profile1", tools);

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task AddProfileToTools_ReturnsFailure_WhenProfileNotFound()
        {
            var tools = new List<string> { "Tool1" };
            var tool = new DbTool { Id = 1, Name = "Tool1" };
            _mockToolRepository.Setup(r => r.GetByNameAsync("Tool1")).ReturnsAsync(tool);
            _mockProfileRepository.Setup(r => r.GetByNameAsync("Profile1")).ReturnsAsync((DbProfile?)null);

            var result = await _profileLogic.AddProfileToTools("Profile1", tools);

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task AddToolToProfiles_ReturnsFailure_WhenToolNotFound()
        {
            _mockToolRepository.Setup(r => r.GetByNameAsync("Tool1")).ReturnsAsync((DbTool?)null);

            var result = await _profileLogic.AddToolToProfiles("Tool1", new List<string> { "Profile1" });

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task AddToolToProfiles_ReturnsSuccess_WhenAdded()
        {
            var tool = new DbTool { Id = 2, Name = "Tool1" };
            _mockToolRepository.Setup(r => r.GetByNameAsync("Tool1")).ReturnsAsync(tool);
            _mockProfileToolsRepository.Setup(r => r.AddAssociationsByToolIdAsync(tool.Id, It.IsAny<List<string>>())).ReturnsAsync(true);

            var result = await _profileLogic.AddToolToProfiles("Tool1", new List<string> { "Profile1" });

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data);
            Assert.Equal("Profile1", result.Data.First());
        }

        [Fact]
        public async Task DeleteProfileAssociations_ReturnsFailure_WhenProfileNotFound()
        {
            _mockProfileRepository.Setup(r => r.GetByNameAsync("Profile1")).ReturnsAsync((DbProfile?)null);

            var result = await _profileLogic.DeleteProfileAssociations("Profile1", new List<string> { "Tool1" });

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task DeleteToolAssociations_ReturnsFailure_WhenToolNotFound()
        {
            _mockToolRepository.Setup(r => r.GetByNameAsync("Tool1")).ReturnsAsync((DbTool?)null);

            var result = await _profileLogic.DeleteToolAssociations("Tool1", new List<string> { "Profile1" });

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task DeleteProfile_ReturnsErrorMessage_WhenProfileDoesNotExist()
        {
            // Arrange
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((DbProfile)null);

            // Act
            var result = await _profileLogic.DeleteProfile("NonExistentProfile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("No profile with the specified name was found. Name: 'NonExistentProfile'", result.ErrorMessage);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task DeleteProfile_DeletesProfile_WhenProfileExists()
        {
            // Arrange
            var profileTools = new List<DbProfileTool> { new DbProfileTool { ProfileID = 1, ToolID = 1, Tool = new DbTool { Name = "Tool1" } } };
            var profile = new DbProfile { Id = 1, Name = "ExistingProfile", Host = AGIServiceHost.Azure.ToString(), ProfileTools = profileTools };

            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockProfileToolsRepository.Setup(repo => repo.DeleteAllProfileAssociationsAsync(It.IsAny<int>())).ReturnsAsync(true);
            _mockProfileRepository.Setup(repo => repo.DeleteAsync(It.IsAny<DbProfile>())).ReturnsAsync(true);

            // Act
            var result = await _profileLogic.DeleteProfile("ExistingProfile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Data);
            _mockProfileRepository.Verify(repo => repo.DeleteAsync(It.IsAny<DbProfile>()), Times.Once);
            _mockProfileToolsRepository.Verify(repo => repo.DeleteAllProfileAssociationsAsync(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task GetAllProfiles_ReturnsEmptyList_WhenNoProfilesExist()
        {
            // Arrange
            _mockProfileRepository.Setup(repo => repo.GetAllAsync(null, null)).ReturnsAsync(new List<DbProfile>());

            // Act
            var result = await _profileLogic.GetAllProfiles(1, 10);

            // Assert
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAllProfiles_ReturnsProfiles_WhenProfilesExist()
        {
            // Arrange
            var profiles = new List<DbProfile> { new DbProfile { Id = 1, Name = "Profile1", Host = AGIServiceHost.Azure.ToString() } };
            var profileTools = new List<DbProfileTool> { new DbProfileTool { ProfileID = 1, Tool = new DbTool { Name = "Tool1" } } };
            var tool = new DbTool { Name = "Tool1" };

            _mockProfileRepository.Setup(repo => repo.GetAllAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(profiles);
            _mockProfileToolsRepository.Setup(repo => repo.GetToolAssociationsAsync(It.IsAny<int>())).ReturnsAsync(profileTools);
            _mockToolRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(tool);

            // Act
            var result = await _profileLogic.GetAllProfiles(1, 10);

            // Assert
            Assert.NotEmpty(result.Data);
            Assert.Single(result.Data);
            Assert.Equal("Profile1", result.Data.First().Name);
            Assert.NotEmpty(result.Data.First().Tools);
            Assert.Equal("Tool1", result.Data.First().Tools.First().Function.Name);
        }
    }
}