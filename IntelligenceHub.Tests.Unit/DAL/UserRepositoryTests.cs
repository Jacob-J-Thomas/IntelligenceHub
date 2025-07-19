using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class UserRepositoryTests
    {
        private IntelligenceHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<IntelligenceHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IntelligenceHubDbContext(options);
        }

        [Fact]
        public async Task GetBySubAsync_ReturnsUser()
        {
            await using var context = CreateContext();
            var user = new DbUser { Sub="sub", ApiToken="token", TenantId=Guid.NewGuid() };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var repo = new UserRepository(context);

            var result = await repo.GetBySubAsync("sub");

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }

        [Fact]
        public async Task GetByApiTokenAsync_ReturnsUser()
        {
            await using var context = CreateContext();
            var user = new DbUser { Sub="s", ApiToken="token", TenantId=Guid.NewGuid() };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var repo = new UserRepository(context);

            var result = await repo.GetByApiTokenAsync("token");

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUser()
        {
            await using var context = CreateContext();
            var user = new DbUser { Sub="s", ApiToken="t", TenantId=Guid.NewGuid(), AccessLevel="Free" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var repo = new UserRepository(context);

            user.AccessLevel = "Pro";
            var updated = await repo.UpdateAsync(user);

            Assert.Equal("Pro", updated.AccessLevel);
            Assert.Equal("Pro", (await context.Users.FindAsync(user.Id))!.AccessLevel);
        }
    }
}
