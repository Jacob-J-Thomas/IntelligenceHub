using IntelligenceHub.Business.Implementations;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Cryptography;
using System.Text;
using System;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class UserLogicTests
    {
        [Fact]
        public async Task GetUserByTenantIdAsync_ReturnsRepositoryResult()
        {
            var config = new ConfigurationBuilder().Build();
            var repo = new Mock<IUserRepository>();
            var user = new DbUser();
            var tenantId = Guid.NewGuid();
            repo.Setup(r => r.GetByTenantIdAsync(tenantId)).ReturnsAsync(user);

            var logic = new UserLogic(repo.Object, config);
            var result = await logic.GetUserByTenantIdAsync(tenantId);

            Assert.Same(user, result);
        }

        [Fact]
        public async Task GetUserByApiTokenAsync_HashesTokenAndQueriesRepository()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "ApiKeyPepper", "pep" } })
                .Build();
            var repo = new Mock<IUserRepository>();
            var user = new DbUser();
            var token = "token";
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes("pep" + token);
            var expectedHash = Convert.ToHexString(sha.ComputeHash(bytes));
            repo.Setup(r => r.GetByApiTokenAsync(expectedHash)).ReturnsAsync(user);

            var logic = new UserLogic(repo.Object, configuration);
            var result = await logic.GetUserByApiTokenAsync(token);

            Assert.Same(user, result);
            repo.Verify(r => r.GetByApiTokenAsync(expectedHash), Times.Once);
        }
    }
}
