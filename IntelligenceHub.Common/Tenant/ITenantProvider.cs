namespace IntelligenceHub.Common.Tenant
{
    public interface ITenantProvider
    {
        Guid? TenantId { get; set; }
    }
}
