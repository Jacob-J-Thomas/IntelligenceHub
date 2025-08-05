using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Tenant
{
    public class TenantProvider : ITenantProvider
    {
        private Guid? _tenantId;
        private DbUser? _user;

        public Guid? TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }

        public DbUser? User
        {
            get => _user;
            set => _user = value;
        }
    }
}
