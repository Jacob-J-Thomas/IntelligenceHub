using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class MessageHistoryRepositoryTests
    {
        private readonly TenantProvider _tenantProvider = new TenantProvider();

        private IntelligenceHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<IntelligenceHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IntelligenceHubDbContext(options);
        }

        private DbMessage CreateMessage(Guid conversationId, DateTime ts)
        {
            return new DbMessage
            {
                ConversationId = conversationId,
                Role = "user",
                TimeStamp = ts,
                Content = "hi",
                TenantId = _tenantProvider.TenantId!.Value
            };
        }

        [Fact]
        public async Task GetConversationAsync_ReturnsOrderedPage()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            var conv = Guid.NewGuid();
            await using var context = CreateContext();
            context.Messages.AddRange(
                CreateMessage(conv, new DateTime(2024,1,1)),
                CreateMessage(conv, new DateTime(2024,1,2)),
                CreateMessage(conv, new DateTime(2024,1,3)),
                CreateMessage(Guid.NewGuid(), DateTime.UtcNow)
            );
            await context.SaveChangesAsync();

            var repo = new MessageHistoryRepository(context, _tenantProvider);
            var results = await repo.GetConversationAsync(conv, 2, 1);

            Assert.Equal(2, results.Count);
            Assert.True(results[0].TimeStamp < results[1].TimeStamp);
        }

        [Fact]
        public async Task DeleteConversationAsync_RemovesMessages()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            var conv = Guid.NewGuid();
            await using var context = CreateContext();
            context.Messages.AddRange(
                CreateMessage(conv, DateTime.UtcNow),
                CreateMessage(conv, DateTime.UtcNow.AddMinutes(1))
            );
            await context.SaveChangesAsync();

            var repo = new MessageHistoryRepository(context, _tenantProvider);
            var result = await repo.DeleteConversationAsync(conv);

            Assert.True(result);
            Assert.Empty(context.Messages.Where(m => m.ConversationId == conv));
        }

        [Fact]
        public async Task DeleteAsync_RemovesSingleMessage()
        {
            _tenantProvider.TenantId = Guid.NewGuid();
            var conv = Guid.NewGuid();
            await using var context = CreateContext();
            var msg = CreateMessage(conv, DateTime.UtcNow);
            context.Messages.Add(msg);
            await context.SaveChangesAsync();

            var repo = new MessageHistoryRepository(context, _tenantProvider);
            var result = await repo.DeleteAsync(conv, msg.Id);

            Assert.True(result);
            Assert.Empty(context.Messages);
        }
    }
}
