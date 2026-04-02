namespace Finans.WebMvc.Security
{
    /// <summary>
    /// Neden var?
    /// - Claim isimleri kodda dağılmasın.
    /// - Yazım hataları yüzünden erişim bug'ı oluşmasın.
    /// </summary>
    public static class ClaimConstants
    {
        public const string CompanyId = "company_id";
    }
}
