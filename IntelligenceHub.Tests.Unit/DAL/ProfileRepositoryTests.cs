using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class ProfileRepositoryTests
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
            var tool = new DbTool { Name = "tool1", Description="d", Required="r", TenantId=_tenantProvider.TenantId!.Value };
            context.Tools.Add(tool);
            await context.SaveChangesAsync();
            var prop = new DbProperty { Name="p", Type="t", Description="d", ToolId=tool.Id, Tool=tool, TenantId=_tenantProvider.TenantId!.Value };
            context.Properties.Add(prop);
            var profile = new DbProfile { Name="profile1", Model="m", Host="h", TenantId=_tenantProvider.TenantId!.Value };
            context.Profiles.Add(profile);
            await context.SaveChangesAsync();
            context.ProfileTools.Add(new DbProfileTool{ ProfileID=profile.Id, ToolID=tool.Id, Profile=profile, Tool=tool, TenantId=_tenantProvider.TenantId!.Value });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsProfileWithTools()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedAsync(context);
            var repo = new ProfileRepository(context, _tenantProvider);

            var result = await repo.GetByNameAsync("profile1");

            Assert.NotNull(result);
            Assert.Single(result!.ProfileTools);
            Assert.Equal("tool1", result.ProfileTools.First().Tool.Name);
            Assert.Single(result.ProfileTools.First().Tool.Properties);
        }

        [Fact]
        public async Task GetAsync_ReturnsProfileById()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedAsync(context);
            var repo = new ProfileRepository(context, _tenantProvider);
            var id = context.Profiles.Single().Id;

            var result = await repo.GetAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.Id);
        }
    }
}
