using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Tenant
{
    public interface ITenantProvider
    {
        Guid? TenantId { get; set; }
        DbUser? User { get; set; }
    }
}
