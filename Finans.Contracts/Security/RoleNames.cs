namespace Finans.Contracts.Security
{
    /// <summary>
    /// Seed data Role.Code değerleriyle birebir uyumludur.
    /// AuthController claim'e Role.Code yazıyor → bu sabitler o değerlerle eşleşmeli.
    /// </summary>
    public static class RoleNames
    {
        public const string Admin = "ADMIN";
        public const string Dealer = "DEALER";
        public const string SubDealer = "SUB_DEALER";
        public const string Accountant = "ACCOUNTANT";
        public const string Company = "COMPANY_ADMIN";
        public const string CompanyUser = "COMPANY_USER";
    }
}
