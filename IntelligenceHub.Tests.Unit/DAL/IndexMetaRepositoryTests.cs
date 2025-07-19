using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class IndexMetaRepositoryTests
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
        public async Task GetByNameAsync_ReturnsItem_ForMatchingTenant()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            var expected = new DbIndexMetadata
            {
                Name = "index1",
                GenerationHost = "host",
                RagHost = "rag",
                IndexingInterval = TimeSpan.FromHours(1),
                TenantId = _tenantProvider.TenantId.Value
            };
            context.IndexMetadata.Add(expected);
            context.IndexMetadata.Add(new DbIndexMetadata
            {
                Name = "index1",
                GenerationHost = "host",
                RagHost = "rag",
                IndexingInterval = TimeSpan.FromHours(1),
                TenantId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var repo = new IndexMetaRepository(context, _tenantProvider);
            var result = await repo.GetByNameAsync("index1");

            Assert.NotNull(result);
            Assert.Equal(expected.Id, result!.Id);
            Assert.Equal(_tenantProvider.TenantId, result.TenantId);
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsNull_WhenNotFound()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            await using var context = CreateContext();
            var repo = new IndexMetaRepository(context, _tenantProvider);

            var result = await repo.GetByNameAsync("missing");

            Assert.Null(result);
        }
    }
}
