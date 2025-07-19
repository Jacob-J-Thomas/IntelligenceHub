using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class ProfileToolsAssociativeRepositoryTests
    {
        private readonly TenantProvider _tenantProvider = new TenantProvider();

        private IntelligenceHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<IntelligenceHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IntelligenceHubDbContext(options);
        }

        private async Task<(DbProfile profile, DbTool tool)> SeedAsync(IntelligenceHubDbContext context)
        {
            var profile = new DbProfile { Name="p1", Model="m", Host="h", TenantId=_tenantProvider.TenantId!.Value };
            var tool = new DbTool { Name="t1", Description="d", Required="r", TenantId=_tenantProvider.TenantId!.Value };
            context.Profiles.Add(profile);
            context.Tools.Add(tool);
            await context.SaveChangesAsync();
            context.ProfileTools.Add(new DbProfileTool { ProfileID=profile.Id, ToolID=tool.Id, Profile=profile, Tool=tool, TenantId=_tenantProvider.TenantId!.Value });
            await context.SaveChangesAsync();
            return (profile, tool);
        }

        [Fact]
        public async Task GetToolAssociationsAsync_ReturnsAssociations()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            var (profile, _) = await SeedAsync(context);
            var repo = new ProfileToolsAssociativeRepository(context, _tenantProvider);

            var result = await repo.GetToolAssociationsAsync(profile.Id);

            Assert.Single(result);
            Assert.Equal(profile.Id, result[0].ProfileID);
        }

        [Fact]
        public async Task AddAssociationsByProfileIdAsync_AddsNewAssociations()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            var (profile, tool) = await SeedAsync(context);
            var tool2 = new DbTool { Name="t2", Description="d", Required="r", TenantId=_tenantProvider.TenantId!.Value };
            context.Tools.Add(tool2);
            await context.SaveChangesAsync();
            var repo = new ProfileToolsAssociativeRepository(context, _tenantProvider);

            var result = await repo.AddAssociationsByProfileIdAsync(profile.Id, new List<int>{tool2.Id});

            Assert.True(result);
            Assert.Equal(2, context.ProfileTools.Count());
        }

        [Fact]
        public async Task DeleteToolAssociationAsync_RemovesAssociation()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            var (profile, tool) = await SeedAsync(context);
            var repo = new ProfileToolsAssociativeRepository(context, _tenantProvider);

            var result = await repo.DeleteToolAssociationAsync(tool.Id, profile.Name);

            Assert.True(result);
            Assert.Empty(context.ProfileTools);
        }
    }
}
