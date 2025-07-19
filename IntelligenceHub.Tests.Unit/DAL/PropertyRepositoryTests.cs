using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class PropertyRepositoryTests
    {
        private readonly TenantProvider _tenantProvider = new TenantProvider();

        private IntelligenceHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<IntelligenceHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IntelligenceHubDbContext(options);
        }

        [Fact]
        public async Task GetToolProperties_ReturnsOnlyForTool()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            var tool1 = new DbTool { Name="t1", Description="d", Required="r", TenantId=_tenantProvider.TenantId.Value };
            var tool2 = new DbTool { Name="t2", Description="d", Required="r", TenantId=_tenantProvider.TenantId.Value };
            context.Tools.AddRange(tool1, tool2);
            await context.SaveChangesAsync();
            context.Properties.Add(new DbProperty { Name="p1", Type="string", Description="d", ToolId=tool1.Id, Tool=tool1, TenantId=_tenantProvider.TenantId.Value });
            context.Properties.Add(new DbProperty { Name="p2", Type="string", Description="d", ToolId=tool2.Id, Tool=tool2, TenantId=_tenantProvider.TenantId.Value });
            await context.SaveChangesAsync();

            var repo = new PropertyRepository(context, _tenantProvider);
            var result = await repo.GetToolProperties(tool1.Id);

            Assert.Single(result);
            Assert.Equal("p1", result.First().Name);
        }
    }
}
