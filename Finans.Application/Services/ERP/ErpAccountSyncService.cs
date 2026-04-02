using Dapper;
using Finans.Application.Abstractions.ERP;
using Finans.Data.Context;
using Finans.Entities.ERP;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Finans.Application.Services.ERP
{
    /// <summary>
    /// Logo Tiger SQL Server'indan hesap planini ceker ve yerel DB'ye yazar.
    /// CompanyErpConnection tablosundaki baglanti bilgileri kullanilir.
    /// Tablo isimleri: LG_{FirmNr}_01_EMUHACC, LG_{FirmNr}_01_CLCARD, LG_{FirmNr}_01_BNCARD
    /// FirmNr appsettings.json LogoTiger:FirmNr'den veya ExtendedProperty01'den alinir.
    /// </summary>
    public sealed class ErpAccountSyncService : IErpAccountSyncService
    {
        private readonly FinansDbContext _db;
        private readonly IConfiguration _configuration;

        public ErpAccountSyncService(FinansDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        private async Task<SqlConnection?> GetErpConnectionAsync(int companyId, CancellationToken ct)
        {
            var conn = await _db.CompanyErpConnections
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, ct);

            if (conn == null) return null;

            var connString = conn.UseIntegratedSecurity
                ? $"Server={conn.Server},{conn.Port ?? 1433};Database={conn.DatabaseName};Integrated Security=True;TrustServerCertificate=True;"
                : $"Server={conn.Server},{conn.Port ?? 1433};Database={conn.DatabaseName};User Id={conn.DbUser};Password={conn.DbPasswordEncrypted};TrustServerCertificate=True;";

            return new SqlConnection(connString);
        }

        /// <summary>
        /// Logo Tiger firma numarasini al.
        /// Oncelik: CompanyErpConnection.ExtendedProperty01 > appsettings LogoTiger:FirmNr > 325
        /// </summary>
        private async Task<string> GetFirmNrAsync(int companyId, CancellationToken ct)
        {
            var conn = await _db.CompanyErpConnections
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, ct);

            if (conn != null && !string.IsNullOrWhiteSpace(conn.ExtendedProperty01))
                return conn.ExtendedProperty01;

            var configFirmNr = _configuration.GetValue<int>("LogoTiger:FirmNr", 325);
            return configFirmNr.ToString("D3"); // 325 -> "325"
        }

        public async Task SyncGlAccountsAsync(int companyId, CancellationToken ct = default)
        {
            using var erpConn = await GetErpConnectionAsync(companyId, ct);
            if (erpConn == null) return;

            await erpConn.OpenAsync(ct);

            var firmNr = await GetFirmNrAsync(companyId, ct);

            // Logo Tiger muhasebe hesap plani: LG_{firmNr}_01_EMUHACC
            var sql = $@"
                SELECT CODE AS GlCode, DEFINITION_ AS GlName
                FROM LG_{firmNr}_01_EMUHACC
                WHERE ACTIVE = 0
                ORDER BY CODE";

            var rows = (await erpConn.QueryAsync<(string GlCode, string GlName)>(sql)).ToList();

            var existingCodes = await _db.ErpGlAccounts
                .Where(x => x.CompanyId == companyId)
                .Select(x => x.GlCode)
                .ToListAsync(ct);

            var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (!existingSet.Contains(row.GlCode))
                {
                    _db.ErpGlAccounts.Add(new ErpGlAccount
                    {
                        CompanyId = companyId,
                        ErpSystemId = 1,
                        GlCode = row.GlCode,
                        GlName = row.GlName,
                        IsActive = true,
                        LastSyncedAtUtc = DateTime.UtcNow,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
                else
                {
                    // Mevcut kaydi guncelle
                    var existing = await _db.ErpGlAccounts
                        .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.GlCode == row.GlCode, ct);
                    if (existing != null)
                    {
                        existing.GlName = row.GlName;
                        existing.LastSyncedAtUtc = DateTime.UtcNow;
                        existing.UpdatedAtUtc = DateTime.UtcNow;
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SyncCurrentAccountsAsync(int companyId, CancellationToken ct = default)
        {
            using var erpConn = await GetErpConnectionAsync(companyId, ct);
            if (erpConn == null) return;

            await erpConn.OpenAsync(ct);

            var firmNr = await GetFirmNrAsync(companyId, ct);

            // Logo Tiger cari hesaplar: LG_{firmNr}_01_CLCARD
            var sql = $@"
                SELECT CODE AS CurrentCode, DEFINITION_ AS CurrentName,
                       TCKNO AS IdentityNumber, TAXNR AS TaxNumber
                FROM LG_{firmNr}_01_CLCARD
                WHERE ACTIVE = 0
                ORDER BY CODE";

            var rows = (await erpConn.QueryAsync<(string CurrentCode, string CurrentName, string? IdentityNumber, string? TaxNumber)>(sql)).ToList();

            var existing = await _db.ErpCurrentAccounts
                .Where(x => x.CompanyId == companyId)
                .Select(x => x.CurrentCode)
                .ToListAsync(ct);

            var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (!existingSet.Contains(row.CurrentCode))
                {
                    _db.ErpCurrentAccounts.Add(new ErpCurrentAccount
                    {
                        CompanyId = companyId,
                        ErpSystemId = 1,
                        CurrentCode = row.CurrentCode,
                        CurrentName = row.CurrentName,
                        IdentityNumber = row.IdentityNumber,
                        TaxNumber = row.TaxNumber,
                        IsActive = true,
                        LastSyncedAtUtc = DateTime.UtcNow,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
                else
                {
                    var existingItem = await _db.ErpCurrentAccounts
                        .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.CurrentCode == row.CurrentCode, ct);
                    if (existingItem != null)
                    {
                        existingItem.CurrentName = row.CurrentName;
                        existingItem.TaxNumber = row.TaxNumber;
                        existingItem.LastSyncedAtUtc = DateTime.UtcNow;
                        existingItem.UpdatedAtUtc = DateTime.UtcNow;
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SyncBankAccountsAsync(int companyId, CancellationToken ct = default)
        {
            using var erpConn = await GetErpConnectionAsync(companyId, ct);
            if (erpConn == null) return;

            await erpConn.OpenAsync(ct);

            var firmNr = await GetFirmNrAsync(companyId, ct);

            // Logo Tiger banka hesaplari: LG_{firmNr}_01_BNCARD
            var sql = $@"
                SELECT CODE AS BankCode, DEFINITION_ AS BankName
                FROM LG_{firmNr}_01_BNCARD
                WHERE ACTIVE = 0
                ORDER BY CODE";

            var rows = (await erpConn.QueryAsync<(string BankCode, string BankName)>(sql)).ToList();

            var existing = await _db.ErpBankAccounts
                .Where(x => x.CompanyId == companyId)
                .Select(x => x.BankAccCode)
                .ToListAsync(ct);

            var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (!existingSet.Contains(row.BankCode))
                {
                    _db.ErpBankAccounts.Add(new ErpBankAccount
                    {
                        CompanyId = companyId,
                        ErpSystemId = 1,
                        BankAccCode = row.BankCode,
                        BankAccName = row.BankName,
                        IsActive = true,
                        LastSyncedAtUtc = DateTime.UtcNow,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
                else
                {
                    var existingItem = await _db.ErpBankAccounts
                        .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BankAccCode == row.BankCode, ct);
                    if (existingItem != null)
                    {
                        existingItem.BankAccName = row.BankName;
                        existingItem.LastSyncedAtUtc = DateTime.UtcNow;
                        existingItem.UpdatedAtUtc = DateTime.UtcNow;
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
