using Finans.Application.Abstractions.ERP;
using Finans.Data.Context;
using Finans.Entities.ERP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace Finans.DesktopConnector.Services
{
    [SupportedOSPlatform("windows")]
    public sealed class LogoTigerAccountSyncService : IErpAccountSyncService
    {
        private readonly FinansDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LogoTigerAccountSyncService> _logger;

        public LogoTigerAccountSyncService(
            FinansDbContext db,
            IConfiguration configuration,
            ILogger<LogoTigerAccountSyncService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SyncGlAccountsAsync(int companyId, CancellationToken ct = default)
        {
            _logger.LogInformation("GL hesap senkronizasyonu basladi. CompanyId={CompanyId}", companyId);

            var erpSystemId = await GetErpSystemIdAsync(companyId, ct);
            if (erpSystemId == 0) return;

            dynamic? unityApp = null;
            try
            {
                unityApp = CreateAndLoginUnity();
                if (unityApp == null) return;

                const int doAccountPlan = 12;
                dynamic? dataObj = unityApp.NewDataObject(doAccountPlan);
                if (dataObj == null)
                {
                    _logger.LogWarning("GL hesap veri nesnesi olusturulamadi.");
                    return;
                }

                dynamic? browser = dataObj.CreateBrowser();
                if (browser == null) return;

                browser.MoveFirst();
                var syncCount = 0;

                while (!browser.EOF)
                {
                    ct.ThrowIfCancellationRequested();

                    // KRITIK: dynamic -> object? -> string? donusumu acik yapilmali
                    // var kullanirsan derleyici sonucu dynamic olarak isler
                    // ve LINQ expression tree icinde CS1963 hatasi verir
                    object? codeRaw = ReadField(browser, "CODE");
                    object? nameRaw = ReadField(browser, "DEFINITION_");
                    string? code = codeRaw?.ToString();
                    string? name = nameRaw?.ToString();

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        string glCode = code;
                        var existing = await _db.ErpGlAccounts
                            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.GlCode == glCode, ct);

                        if (existing == null)
                        {
                            _db.ErpGlAccounts.Add(new ErpGlAccount
                            {
                                CompanyId = companyId,
                                ErpSystemId = erpSystemId,
                                GlCode = code,
                                GlName = name ?? code,
                                IsActive = true,
                                LastSyncedAtUtc = DateTime.UtcNow,
                                CreatedAtUtc = DateTime.UtcNow,
                                IsDeleted = false
                            });
                        }
                        else
                        {
                            existing.GlName = name ?? existing.GlName;
                            existing.LastSyncedAtUtc = DateTime.UtcNow;
                            existing.UpdatedAtUtc = DateTime.UtcNow;
                        }
                        syncCount++;
                    }

                    browser.MoveNext();
                }

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("GL hesap senkronizasyonu tamamlandi. {Count} hesap.", syncCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GL hesap senkronizasyon hatasi.");
            }
            finally
            {
                SafeLogout(unityApp);
            }
        }

        public async Task SyncCurrentAccountsAsync(int companyId, CancellationToken ct = default)
        {
            _logger.LogInformation("Cari hesap senkronizasyonu basladi. CompanyId={CompanyId}", companyId);

            var erpSystemId = await GetErpSystemIdAsync(companyId, ct);
            if (erpSystemId == 0) return;

            dynamic? unityApp = null;
            try
            {
                unityApp = CreateAndLoginUnity();
                if (unityApp == null) return;

                const int doArpCard = 18;
                dynamic? dataObj = unityApp.NewDataObject(doArpCard);
                if (dataObj == null) return;

                dynamic? browser = dataObj.CreateBrowser();
                if (browser == null) return;

                browser.MoveFirst();
                var syncCount = 0;

                while (!browser.EOF)
                {
                    ct.ThrowIfCancellationRequested();

                    object? codeRaw = ReadField(browser, "CODE");
                    object? nameRaw = ReadField(browser, "DEFINITION_");
                    object? taxNrRaw = ReadField(browser, "TAX_NR");
                    string? code = codeRaw?.ToString();
                    string? name = nameRaw?.ToString();
                    string? taxNr = taxNrRaw?.ToString();

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        string currentCode = code;
                        var existing = await _db.ErpCurrentAccounts
                            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.CurrentCode == currentCode, ct);

                        if (existing == null)
                        {
                            _db.ErpCurrentAccounts.Add(new ErpCurrentAccount
                            {
                                CompanyId = companyId,
                                ErpSystemId = erpSystemId,
                                CurrentCode = code,
                                CurrentName = name ?? code,
                                TaxNumber = taxNr,
                                IsActive = true,
                                LastSyncedAtUtc = DateTime.UtcNow,
                                CreatedAtUtc = DateTime.UtcNow,
                                IsDeleted = false
                            });
                        }
                        else
                        {
                            existing.CurrentName = name ?? existing.CurrentName;
                            existing.TaxNumber = taxNr ?? existing.TaxNumber;
                            existing.LastSyncedAtUtc = DateTime.UtcNow;
                            existing.UpdatedAtUtc = DateTime.UtcNow;
                        }
                        syncCount++;
                    }

                    browser.MoveNext();
                }

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Cari hesap senkronizasyonu tamamlandi. {Count} hesap.", syncCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari hesap senkronizasyon hatasi.");
            }
            finally
            {
                SafeLogout(unityApp);
            }
        }

        public async Task SyncBankAccountsAsync(int companyId, CancellationToken ct = default)
        {
            _logger.LogInformation("Banka hesap senkronizasyonu basladi. CompanyId={CompanyId}", companyId);

            var erpSystemId = await GetErpSystemIdAsync(companyId, ct);
            if (erpSystemId == 0) return;

            dynamic? unityApp = null;
            try
            {
                unityApp = CreateAndLoginUnity();
                if (unityApp == null) return;

                const int doBankAccount = 62;
                dynamic? dataObj = unityApp.NewDataObject(doBankAccount);
                if (dataObj == null) return;

                dynamic? browser = dataObj.CreateBrowser();
                if (browser == null) return;

                browser.MoveFirst();
                var syncCount = 0;

                while (!browser.EOF)
                {
                    ct.ThrowIfCancellationRequested();

                    object? codeRaw = ReadField(browser, "CODE");
                    object? nameRaw = ReadField(browser, "DEFINITION_");
                    object? ibanRaw = ReadField(browser, "IBAN");
                    string? code = codeRaw?.ToString();
                    string? name = nameRaw?.ToString();
                    string? iban = ibanRaw?.ToString();

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        string bankAccCode = code;
                        var existing = await _db.ErpBankAccounts
                            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BankAccCode == bankAccCode, ct);

                        if (existing == null)
                        {
                            _db.ErpBankAccounts.Add(new ErpBankAccount
                            {
                                CompanyId = companyId,
                                ErpSystemId = erpSystemId,
                                BankAccCode = code,
                                BankAccName = name ?? code,
                                Iban = iban,
                                IsActive = true,
                                LastSyncedAtUtc = DateTime.UtcNow,
                                CreatedAtUtc = DateTime.UtcNow,
                                IsDeleted = false
                            });
                        }
                        else
                        {
                            existing.BankAccName = name ?? existing.BankAccName;
                            existing.Iban = iban ?? existing.Iban;
                            existing.LastSyncedAtUtc = DateTime.UtcNow;
                            existing.UpdatedAtUtc = DateTime.UtcNow;
                        }
                        syncCount++;
                    }

                    browser.MoveNext();
                }

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Banka hesap senkronizasyonu tamamlandi. {Count} hesap.", syncCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka hesap senkronizasyon hatasi.");
            }
            finally
            {
                SafeLogout(unityApp);
            }
        }

        private dynamic? CreateAndLoginUnity()
        {
            var unityType = Type.GetTypeFromProgID("UnityObjects.UnityApplication");
            if (unityType == null)
            {
                _logger.LogError("Logo Tiger COM bileseni bulunamadi.");
                return null;
            }

            dynamic? unityApp = Activator.CreateInstance(unityType);
            if (unityApp == null) return null;

            var userName = _configuration["LogoTiger:UserName"];
            var password = _configuration["LogoTiger:Password"];
            var firmNr = _configuration.GetValue<int>("LogoTiger:FirmNr");
            var periodNr = _configuration.GetValue<int>("LogoTiger:PeriodNr");

            var ok = unityApp.Login(userName, password, firmNr, periodNr);
            if (!ok)
            {
                _logger.LogError("Logo Tiger login basarisiz: {Error}", (string?)unityApp.GetLastErrorString());
                return null;
            }

            return unityApp;
        }

        private async Task<int> GetErpSystemIdAsync(int companyId, CancellationToken ct)
        {
            var conn = await _db.CompanyErpConnections
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && !x.IsDeleted, ct);

            if (conn != null) return conn.ErpSystemId;

            var logo = await _db.ErpSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == "LOGO_TIGER" && x.IsActive && !x.IsDeleted, ct);

            return logo?.Id ?? 1;
        }

        private static object? ReadField(dynamic browser, string fieldName)
        {
            try { return browser.FieldByName(fieldName)?.Value; }
            catch { return null; }
        }

        private static void SafeLogout(dynamic? unityApp)
        {
            try
            {
                if (unityApp != null)
                {
                    unityApp.CompanyLogout();
                    unityApp.UserLogout();
                }
            }
            catch { }
        }
    }
}
