using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Tenant
{
    public class TenantProvider : ITenantProvider
    {
        private static readonly AsyncLocal<Guid?> _currentTenant = new();
        private static readonly AsyncLocal<DbUser?> _currentUser = new();

        public Guid? TenantId
        {
            get => _currentTenant.Value;
            set => _currentTenant.Value = value;
        }

        public DbUser? User
        {
            get => _currentUser.Value;
            set => _currentUser.Value = value;
        }
    }
}
