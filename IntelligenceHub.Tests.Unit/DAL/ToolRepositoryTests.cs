using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class ToolRepositoryTests
    {
        private readonly TenantProvider _tenantProvider = new TenantProvider();

        private IntelligenceHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<IntelligenceHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IntelligenceHubDbContext(options);
        }

        private async Task SeedAsync(IntelligenceHubDbContext context)
        {
            var tool1 = new DbTool { Name="t1", Description="d", Required="r", TenantId=_tenantProvider.TenantId!.Value };
            var tool2 = new DbTool { Name="t2", Description="d", Required="r", TenantId=_tenantProvider.TenantId!.Value };
            context.Tools.AddRange(tool1, tool2);
            await context.SaveChangesAsync();
            var profile = new DbProfile { Name="p1", Model="m", Host="h", TenantId=_tenantProvider.TenantId!.Value };
            context.Profiles.Add(profile);
            await context.SaveChangesAsync();
            context.ProfileTools.AddRange(
                new DbProfileTool { ProfileID=profile.Id, ToolID=tool1.Id, Profile=profile, Tool=tool1, TenantId=_tenantProvider.TenantId!.Value },
                new DbProfileTool { ProfileID=profile.Id, ToolID=tool2.Id, Profile=profile, Tool=tool2, TenantId=_tenantProvider.TenantId!.Value }
            );
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsTool()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedAsync(context);
            var repo = new ToolRepository(context, _tenantProvider);

            var result = await repo.GetByNameAsync("t1");

            Assert.NotNull(result);
            Assert.Equal("t1", result!.Name);
        }

        [Fact]
        public async Task GetProfileToolsAsync_ReturnsToolNames()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedAsync(context);
            var repo = new ToolRepository(context, _tenantProvider);

            var result = await repo.GetProfileToolsAsync("p1");

            Assert.Equal(2, result.Count);
            Assert.Contains("t1", result);
            Assert.Contains("t2", result);
        }

        [Fact]
        public async Task GetToolProfilesAsync_ReturnsProfileNames()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedAsync(context);
            var repo = new ToolRepository(context, _tenantProvider);

            var result = await repo.GetToolProfilesAsync("t1");

            Assert.Single(result);
            Assert.Equal("p1", result[0]);
        }
    }
}
