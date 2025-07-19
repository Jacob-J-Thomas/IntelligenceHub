using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class UsageServiceTests
    {
        [Fact]
        public async Task ValidateAndIncrementUsageAsync_ReturnsFailure_WhenRateLimited()
        {
            var repo = new Mock<IUserRepository>();
            var rate = new Mock<IRateLimitService>();
            var user = new DbUser { Id = 1, AccessLevel = "Free" };
            rate.Setup(r => r.IsRequestAllowed("1", false)).Returns(false);
            var service = new UsageService(repo.Object, rate.Object);

            var result = await service.ValidateAndIncrementUsageAsync(user);

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.TooManyRequests, result.StatusCode);
            repo.Verify(r => r.UpdateAsync(It.IsAny<DbUser>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAndIncrementUsageAsync_PaidUser_BypassesUpdates()
        {
            var repo = new Mock<IUserRepository>();
            var rate = new Mock<IRateLimitService>();
            var user = new DbUser { Id = 2, AccessLevel = "Paid" };
            rate.Setup(r => r.IsRequestAllowed("2", true)).Returns(true);
            var service = new UsageService(repo.Object, rate.Object);

            var result = await service.ValidateAndIncrementUsageAsync(user);

            Assert.True(result.IsSuccess);
            repo.Verify(r => r.UpdateAsync(It.IsAny<DbUser>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAndIncrementUsageAsync_ResetsMonth_WhenNewMonth()
        {
            var repo = new Mock<IUserRepository>();
            var rate = new Mock<IRateLimitService>();
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            var user = new DbUser { Id = 3, AccessLevel = "Free", RequestsThisMonth = 0, RequestMonthStart = lastMonth };
            rate.Setup(r => r.IsRequestAllowed("3", false)).Returns(true);
            repo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);
            var service = new UsageService(repo.Object, rate.Object);

            await service.ValidateAndIncrementUsageAsync(user);

            var expectedStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            Assert.Equal(expectedStart, user.RequestMonthStart);
            Assert.Equal(1, user.RequestsThisMonth);
            repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ValidateAndIncrementUsageAsync_ReturnsFailure_WhenMonthlyLimitExceeded()
        {
            var repo = new Mock<IUserRepository>();
            var rate = new Mock<IRateLimitService>();
            var user = new DbUser { Id = 4, AccessLevel = "Free", RequestsThisMonth = FreeTierMonthlyLimit, RequestMonthStart = DateTime.UtcNow };
            rate.Setup(r => r.IsRequestAllowed("4", false)).Returns(true);
            var service = new UsageService(repo.Object, rate.Object);

            var result = await service.ValidateAndIncrementUsageAsync(user);

            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.TooManyRequests, result.StatusCode);
        }

        [Fact]
        public async Task ValidateAndIncrementUsageAsync_Increments_WhenAllowed()
        {
            var repo = new Mock<IUserRepository>();
            var rate = new Mock<IRateLimitService>();
            var user = new DbUser { Id = 5, AccessLevel = "Free", RequestsThisMonth = 1, RequestMonthStart = DateTime.UtcNow };
            rate.Setup(r => r.IsRequestAllowed("5", false)).Returns(true);
            repo.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);
            var service = new UsageService(repo.Object, rate.Object);

            var result = await service.ValidateAndIncrementUsageAsync(user);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, user.RequestsThisMonth);
            repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }
    }
}
