namespace IntelligenceHub.Common.Tenant
{
    public interface ITenantProvider
    {
        Guid? TenantId { get; set; }
        DAL.Models.DbUser? User { get; set; }
    }
}
