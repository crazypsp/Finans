using Finans.Entities.Common;

namespace Finans.Entities.Banking
{
    public class BankCredential : AuditableEntity
    {
        // CompanyId, CreatedAtUtc, IsDeleted AuditableEntity'den gelir

        public int BankId { get; set; }

        public int? UserId { get; set; }
        public int? RoleId { get; set; }

        public IntegrationTechnology Technology { get; set; } = IntegrationTechnology.Api;
        public AuthMethod AuthenticationMethod { get; set; } = AuthMethod.Basic;

        public string? ApiBaseUrl { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        public string SecretEncrypted { get; set; } = null!;

        public string? ExtrasJson { get; set; }

        public DateTime? LastUsedAtUtc { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
