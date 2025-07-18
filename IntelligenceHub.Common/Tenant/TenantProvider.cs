using System.Threading;

namespace IntelligenceHub.Common.Tenant
{
    public class TenantProvider : ITenantProvider
    {
        private static readonly AsyncLocal<Guid?> _currentTenant = new();

        public Guid? TenantId
        {
            get => _currentTenant.Value;
            set => _currentTenant.Value = value;
        }
    }
}
