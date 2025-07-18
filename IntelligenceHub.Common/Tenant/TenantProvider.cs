using System.Threading;

namespace IntelligenceHub.Common.Tenant
{
    public class TenantProvider : ITenantProvider
    {
        private static readonly AsyncLocal<Guid?> _currentTenant = new();
        private static readonly AsyncLocal<DAL.Models.DbUser?> _currentUser = new();

        public Guid? TenantId
        {
            get => _currentTenant.Value;
            set => _currentTenant.Value = value;
        }

        public DAL.Models.DbUser? User
        {
            get => _currentUser.Value;
            set => _currentUser.Value = value;
        }
    }
}
