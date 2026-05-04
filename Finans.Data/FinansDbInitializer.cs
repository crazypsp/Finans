using Finans.Data.Context;
using Finans.Entities.Banking;
using Finans.Entities.Common;
using Finans.Entities.ERP;
using Finans.Entities.Identity;
using Finans.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Finans.Data
{
    public static class FinansDbInitializer
    {
        private static readonly DateTime SeedTime = new(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc);
        private const string SeedSalt = "AQIDBAUGBwgJCgsMDQ4PEA==";
        private const string InitialMigrationEvidenceTable = "AuditLogs";
        private const string MigrationHistoryTable = "__EFMigrationsHistory";

        public static async Task InitializeFinansDbAsync(this IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();

            if (!await db.Database.CanConnectAsync(ct))
            {
                await db.Database.MigrateAsync(ct);
                await SeedAsync(db, ct);
                return;
            }

            await db.Database.OpenConnectionAsync(ct);
            var appLockAcquired = false;

            try
            {
                appLockAcquired = await TryAcquireInitializationLockAsync(db, ct);
                await EnsureMigrationHistoryForExistingSchemaAsync(db, ct);
                await db.Database.MigrateAsync(ct);
                await SeedAsync(db, ct);
            }
            finally
            {
                if (appLockAcquired)
                    await ReleaseInitializationLockAsync(db, ct);

                await db.Database.CloseConnectionAsync();
            }
        }

        private static async Task EnsureMigrationHistoryForExistingSchemaAsync(FinansDbContext db, CancellationToken ct)
        {
            var migrations = db.Database.GetMigrations().ToArray();
            if (migrations.Length == 0)
                return;

            var initialMigration = migrations[0];
            var hasInitialTable = await TableExistsAsync(db, InitialMigrationEvidenceTable, ct);

            if (!hasInitialTable)
                return;

            await EnsureMigrationHistoryTableAsync(db, ct);

            var initialMigrationAlreadyApplied = await MigrationHistoryContainsAsync(db, initialMigration, ct);
            if (initialMigrationAlreadyApplied)
                return;

            await InsertMigrationHistoryAsync(db, initialMigration, ct);
        }

        private static async Task<bool> TryAcquireInitializationLockAsync(FinansDbContext db, CancellationToken ct)
        {
            var result = await ExecuteScalarAsync<int>(
                db,
                """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = N'FinansDbInitializer',
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Session',
                    @LockTimeout = 60000;
                SELECT @result;
                """,
                ct);

            if (result >= 0)
                return true;

            throw new InvalidOperationException("Finans veritabani baslangic kilidi alinamadi.");
        }

        private static Task ReleaseInitializationLockAsync(FinansDbContext db, CancellationToken ct)
            => ExecuteNonQueryAsync(
                db,
                """
                EXEC sp_releaseapplock
                    @Resource = N'FinansDbInitializer',
                    @LockOwner = N'Session';
                """,
                ct);

        private static Task EnsureMigrationHistoryTableAsync(FinansDbContext db, CancellationToken ct)
            => ExecuteNonQueryAsync(
                db,
                """
                IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32) NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );
                END
                """,
                ct);

        private static async Task<bool> TableExistsAsync(FinansDbContext db, string tableName, CancellationToken ct)
        {
            var count = await ExecuteScalarAsync<int>(
                db,
                "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName AND is_ms_shipped = 0;",
                ct,
                ("@tableName", tableName));

            return count > 0;
        }

        private static async Task<bool> MigrationHistoryContainsAsync(
            FinansDbContext db,
            string migrationId,
            CancellationToken ct)
        {
            var count = await ExecuteScalarAsync<int>(
                db,
                $"SELECT COUNT(1) FROM [{MigrationHistoryTable}] WHERE [MigrationId] = @migrationId;",
                ct,
                ("@migrationId", migrationId));

            return count > 0;
        }

        private static Task InsertMigrationHistoryAsync(
            FinansDbContext db,
            string migrationId,
            CancellationToken ct)
            => ExecuteNonQueryAsync(
                db,
                $"""
                IF NOT EXISTS (SELECT 1 FROM [{MigrationHistoryTable}] WHERE [MigrationId] = @migrationId)
                BEGIN
                    INSERT INTO [{MigrationHistoryTable}] ([MigrationId], [ProductVersion])
                    VALUES (@migrationId, @productVersion);
                END
                """,
                ct,
                ("@migrationId", migrationId),
                ("@productVersion", GetEfProductVersion()));

        private static string GetEfProductVersion()
        {
            var version = typeof(DbContext).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            return string.IsNullOrWhiteSpace(version)
                ? "8.0.0"
                : version.Split('+')[0];
        }

        private static async Task<T> ExecuteScalarAsync<T>(
            FinansDbContext db,
            string sql,
            CancellationToken ct,
            params (string Name, object? Value)[] parameters)
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var command = CreateCommand(connection, sql, parameters);
            var value = await command.ExecuteScalarAsync(ct);

            if (value == null || value == DBNull.Value)
                return default!;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        private static async Task ExecuteNonQueryAsync(
            FinansDbContext db,
            string sql,
            CancellationToken ct,
            params (string Name, object? Value)[] parameters)
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var command = CreateCommand(connection, sql, parameters);
            await command.ExecuteNonQueryAsync(ct);
        }

        private static DbCommand CreateCommand(
            DbConnection connection,
            string sql,
            params (string Name, object? Value)[] parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            return command;
        }

        private static async Task SeedAsync(FinansDbContext db, CancellationToken ct)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var company = await EnsureDemoCompanyAsync(db, ct);
            await EnsureErpSystemAsync(db, ct);

            var roles = await EnsureRolesAsync(db, ct);
            var users = await EnsureUsersAsync(db, ct);
            await EnsureCompanyUsersAsync(db, company, users, ct);
            await EnsureUserRolesAsync(db, company, roles, users, ct);

            var menus = await EnsureMenusAsync(db, ct);
            await EnsureRoleMenuPermissionsAsync(db, roles, menus, ct);

            await EnsureDemoBankingAsync(db, company, ct);

            await tx.CommitAsync(ct);
        }

        private static async Task<Company> EnsureDemoCompanyAsync(FinansDbContext db, CancellationToken ct)
        {
            var company = await db.Companies.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.PrimaryDomain == "demo.finans.local", ct);

            if (company == null)
            {
                company = new Company
                {
                    Name = "Demo Firma A.Ş.",
                    CompanyType = CompanyType.ClientCompany,
                    PrimaryDomain = "demo.finans.local",
                    CountryIso2 = "TR",
                    DefaultCultureName = "tr-TR",
                    TimeZoneId = "Europe/Istanbul",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAtUtc = SeedTime
                };
                db.Companies.Add(company);
            }
            else
            {
                company.Name = "Demo Firma A.Ş.";
                company.CompanyType = CompanyType.ClientCompany;
                company.CountryIso2 = "TR";
                company.DefaultCultureName = "tr-TR";
                company.TimeZoneId = "Europe/Istanbul";
                company.IsActive = true;
                company.IsDeleted = false;
            }

            await db.SaveChangesAsync(ct);
            return company;
        }

        private static async Task EnsureErpSystemAsync(FinansDbContext db, CancellationToken ct)
        {
            var erp = await db.ErpSystems.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == "LOGO_TIGER", ct);

            if (erp == null)
            {
                db.ErpSystems.Add(new ErpSystem
                {
                    Name = "Logo Tiger",
                    Code = "LOGO_TIGER",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAtUtc = SeedTime
                });
            }
            else
            {
                erp.Name = "Logo Tiger";
                erp.IsActive = true;
                erp.IsDeleted = false;
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task<Dictionary<string, Role>> EnsureRolesAsync(FinansDbContext db, CancellationToken ct)
        {
            var seeds = new[]
            {
                new RoleSeed(RoleCodes.ADMIN, "Sistem Yöneticisi", "Tüm yetkilere sahip sistem yöneticisi", true),
                new RoleSeed(RoleCodes.DEALER, "Bayi", "Bayi kullanıcısı", false),
                new RoleSeed(RoleCodes.SUB_DEALER, "Alt Bayi", "Alt bayi kullanıcısı", false),
                new RoleSeed(RoleCodes.ACCOUNTANT, "Muhasebeci", "Muhasebe firması / SMMM", false),
                new RoleSeed(RoleCodes.COMPANY_ADMIN, "Firma Yöneticisi", "Firma yöneticisi", false),
                new RoleSeed(RoleCodes.COMPANY_USER, "Firma Kullanıcısı", "Firma standart kullanıcı", false)
            };
            var seedCodes = seeds.Select(s => s.Code).ToArray();

            foreach (var seed in seeds)
            {
                var role = await db.Roles.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Code == seed.Code, ct);

                if (role == null)
                {
                    db.Roles.Add(new Role
                    {
                        Code = seed.Code,
                        Name = seed.Name,
                        Description = seed.Description,
                        IsSystemRole = seed.IsSystemRole,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAtUtc = SeedTime
                    });
                }
                else
                {
                    role.Name = seed.Name;
                    role.Description = seed.Description;
                    role.IsSystemRole = seed.IsSystemRole;
                    role.IsActive = true;
                    role.IsDeleted = false;
                }
            }

            await db.SaveChangesAsync(ct);

            var roles = await db.Roles.IgnoreQueryFilters().ToListAsync(ct);
            return roles
                .Where(x => seedCodes.Contains(x.Code))
                .ToDictionary(x => x.Code, x => x);
        }

        private static async Task<Dictionary<string, User>> EnsureUsersAsync(FinansDbContext db, CancellationToken ct)
        {
            var seeds = new[]
            {
                new UserSeed("admin", "admin@finans.local", "Sistem", "Yönetici", "HcXuAK10SjWLJPp2fjPLGDZzYBUHjuLTj/VUPgmfEuE=", true),
                new UserSeed("system", "system@finans.local", "Sistem", "Servisi", "tZ6GcKBQNHdPy4lKFkUbrqEP7oFIotGX4NTy/o1qTYE=", false),
                new UserSeed("muhasebe", "muhasebe@finans.local", "Muhasebe", "Kullanıcı", "7kRWXzTNa95r4b/Yz/Gs764Xa+vhx28hJOiAH7Ul4k8=", false),
                new UserSeed("operator", "operator@finans.local", "Finans", "Operator", "5w11B56xzOpVrDfIB5YlMT/ibbmsIAAxKi+GT8/p4ls=", false),
                new UserSeed("izleyici", "izleyici@finans.local", "Finans", "Izleyici", "sfecl/+wAtOelrVn2k0al1DzR1GazfL3LBjYERdixbY=", false)
            };
            var seedUserNames = seeds.Select(s => s.UserName).ToArray();

            foreach (var seed in seeds)
            {
                var user = await db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.UserName == seed.UserName || x.Email == seed.Email, ct);

                if (user == null)
                {
                    db.Users.Add(new User
                    {
                        UserName = seed.UserName,
                        Email = seed.Email,
                        FirstName = seed.FirstName,
                        LastName = seed.LastName,
                        PasswordHash = seed.PasswordHash,
                        PasswordSalt = SeedSalt,
                        IsActive = true,
                        IsEmailVerified = true,
                        IsSystemAdmin = seed.IsSystemAdmin,
                        IsDeleted = false,
                        CreatedAtUtc = SeedTime
                    });
                }
                else
                {
                    user.UserName = seed.UserName;
                    user.Email = seed.Email;
                    user.FirstName = seed.FirstName;
                    user.LastName = seed.LastName;
                    user.PasswordHash = seed.PasswordHash;
                    user.PasswordSalt = SeedSalt;
                    user.IsActive = true;
                    user.IsEmailVerified = true;
                    user.IsSystemAdmin = seed.IsSystemAdmin;
                    user.IsDeleted = false;
                }
            }

            await db.SaveChangesAsync(ct);

            var users = await db.Users.IgnoreQueryFilters().ToListAsync(ct);
            return users
                .Where(x => seedUserNames.Contains(x.UserName))
                .ToDictionary(x => x.UserName, x => x);
        }

        private static async Task EnsureCompanyUsersAsync(
            FinansDbContext db,
            Company company,
            IReadOnlyDictionary<string, User> users,
            CancellationToken ct)
        {
            await EnsureCompanyUserAsync(db, company.Id, users["admin"].Id, true, ct);
            await EnsureCompanyUserAsync(db, company.Id, users["system"].Id, true, ct);
            await EnsureCompanyUserAsync(db, company.Id, users["muhasebe"].Id, false, ct);
            await EnsureCompanyUserAsync(db, company.Id, users["operator"].Id, false, ct);
            await EnsureCompanyUserAsync(db, company.Id, users["izleyici"].Id, false, ct);
            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsureCompanyUserAsync(
            FinansDbContext db,
            int companyId,
            int userId,
            bool isCompanyAdmin,
            CancellationToken ct)
        {
            var companyUser = await db.CompanyUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId, ct);

            if (companyUser == null)
            {
                db.CompanyUsers.Add(new CompanyUser
                {
                    CompanyId = companyId,
                    UserId = userId,
                    IsCompanyAdmin = isCompanyAdmin,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAtUtc = SeedTime
                });
            }
            else
            {
                companyUser.IsCompanyAdmin = isCompanyAdmin;
                companyUser.IsActive = true;
                companyUser.IsDeleted = false;
            }
        }

        private static async Task EnsureUserRolesAsync(
            FinansDbContext db,
            Company company,
            IReadOnlyDictionary<string, Role> roles,
            IReadOnlyDictionary<string, User> users,
            CancellationToken ct)
        {
            await EnsureUserRoleAsync(db, users["admin"].Id, roles[RoleCodes.ADMIN].Id, null, users["admin"].Id, ct);
            await EnsureUserRoleAsync(db, users["system"].Id, roles[RoleCodes.COMPANY_ADMIN].Id, company.Id, users["admin"].Id, ct);
            await EnsureUserRoleAsync(db, users["muhasebe"].Id, roles[RoleCodes.ACCOUNTANT].Id, company.Id, users["admin"].Id, ct);
            await EnsureUserRoleAsync(db, users["operator"].Id, roles[RoleCodes.ACCOUNTANT].Id, company.Id, users["admin"].Id, ct);
            await EnsureUserRoleAsync(db, users["izleyici"].Id, roles[RoleCodes.COMPANY_USER].Id, company.Id, users["admin"].Id, ct);
            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsureUserRoleAsync(
            FinansDbContext db,
            int userId,
            int roleId,
            int? scopeCompanyId,
            int assignedByUserId,
            CancellationToken ct)
        {
            var userRole = await db.UserRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId && x.ScopeCompanyId == scopeCompanyId, ct);

            if (userRole == null)
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    ScopeCompanyId = scopeCompanyId,
                    AssignedAtUtc = SeedTime,
                    AssignedByUserId = assignedByUserId,
                    IsDeleted = false,
                    CreatedAtUtc = SeedTime
                });
            }
            else
            {
                userRole.AssignedByUserId = assignedByUserId;
                userRole.IsDeleted = false;
            }
        }

        private static async Task<Dictionary<string, MenuItem>> EnsureMenusAsync(FinansDbContext db, CancellationToken ct)
        {
            var seeds = MenuSeeds();
            var seedCodes = seeds.Select(s => s.Code).ToArray();

            foreach (var seed in seeds)
            {
                var menu = await db.MenuItems.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Code == seed.Code, ct);

                if (menu == null)
                {
                    db.MenuItems.Add(new MenuItem
                    {
                        Title = seed.Title,
                        Code = seed.Code,
                        Route = seed.Route,
                        Icon = seed.Icon,
                        SortOrder = seed.SortOrder,
                        IsVisible = true,
                        IsDeleted = false,
                        CreatedAtUtc = SeedTime
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            var menuItems = await db.MenuItems.IgnoreQueryFilters().ToListAsync(ct);
            var menus = menuItems
                .Where(x => seedCodes.Contains(x.Code))
                .ToDictionary(x => x.Code, x => x);

            foreach (var seed in seeds)
            {
                var menu = menus[seed.Code];
                menu.Title = seed.Title;
                menu.Route = seed.Route;
                menu.Icon = seed.Icon;
                menu.SortOrder = seed.SortOrder;
                menu.IsVisible = true;
                menu.IsDeleted = false;
                menu.ParentMenuItemId = seed.ParentCode == null ? null : menus[seed.ParentCode].Id;
            }

            await db.SaveChangesAsync(ct);
            return menus;
        }

        private static async Task EnsureRoleMenuPermissionsAsync(
            FinansDbContext db,
            IReadOnlyDictionary<string, Role> roles,
            IReadOnlyDictionary<string, MenuItem> menus,
            CancellationToken ct)
        {
            await EnsurePermissionsAsync(db, roles[RoleCodes.ADMIN].Id, menus.Values.Select(x => x.Id), PermissionPreset.Full, ct);
            await EnsurePermissionsAsync(db, roles[RoleCodes.ACCOUNTANT].Id, MenuIds(menus, "DASHBOARD", "BANK_OPS", "BANK_TRANSACTIONS", "BANK_MANAGEMENT", "ERP_TRANSFER", "ERP_BATCH", "ERP_FAILED", "DEFINITIONS", "ERP_MAPPING", "RULES", "REPORTS", "SYSTEM_LOGS"), PermissionPreset.EditorNoDelete, ct);
            await EnsurePermissionsAsync(db, roles[RoleCodes.COMPANY_ADMIN].Id, MenuIds(menus, "DASHBOARD", "BANK_OPS", "BANK_TRANSACTIONS", "ERP_TRANSFER", "ERP_BATCH", "ERP_FAILED", "REPORTS"), PermissionPreset.EditorNoDelete, ct);
            await EnsurePermissionsAsync(db, roles[RoleCodes.COMPANY_USER].Id, MenuIds(menus, "DASHBOARD", "BANK_OPS", "BANK_TRANSACTIONS", "REPORTS"), PermissionPreset.ReadOnlyExport, ct);
            await EnsurePermissionsAsync(db, roles[RoleCodes.DEALER].Id, MenuIds(menus, "DASHBOARD", "REPORTS", "CONNECTOR"), PermissionPreset.ReadOnlyExport, ct);
            await db.SaveChangesAsync(ct);
        }

        private static IEnumerable<int> MenuIds(IReadOnlyDictionary<string, MenuItem> menus, params string[] codes)
            => codes.Select(code => menus[code].Id);

        private static async Task EnsurePermissionsAsync(
            FinansDbContext db,
            int roleId,
            IEnumerable<int> menuIds,
            PermissionPreset preset,
            CancellationToken ct)
        {
            foreach (var menuId in menuIds)
            {
                var permission = await db.RoleMenuPermissions.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.RoleId == roleId && x.MenuItemId == menuId, ct);

                if (permission == null)
                {
                    db.RoleMenuPermissions.Add(new RoleMenuPermission
                    {
                        RoleId = roleId,
                        MenuItemId = menuId,
                        CreatedAtUtc = SeedTime
                    });
                    permission = db.RoleMenuPermissions.Local.Last();
                }

                permission.CanView = preset.CanView;
                permission.CanCreate = preset.CanCreate;
                permission.CanUpdate = preset.CanUpdate;
                permission.CanDelete = preset.CanDelete;
                permission.CanApprove = preset.CanApprove;
                permission.CanExport = preset.CanExport;
                permission.IsDeleted = false;
            }
        }

        private static async Task EnsureDemoBankingAsync(FinansDbContext db, Company company, CancellationToken ct)
        {
            var bankSeeds = new[]
            {
                new BankSeed("ALB", "Albaraka Türk", 1, false, false, true, null, null),
                new BankSeed("AKB", "Akbank", 3, false, false, true, null, null),
                new BankSeed("ANB", "Anadolubank", 4, false, false, true, null, null),
                new BankSeed("DEN", "Denizbank", 6, false, false, true, null, null),
                new BankSeed("EML", "Emlak Katılım", 12, false, false, true, null, null),
                new BankSeed("ISB", "İş Bankası", 2, true, true, true, null, null),
                new BankSeed("KTB", "Kuveyt Türk", 5, false, false, true, null, null),
                new BankSeed("QNB", "QNB Finansbank", 8, false, false, true, null, null),
                new BankSeed("SKR", "Şekerbank", 10, false, false, true, null, null),
                new BankSeed("TFN", "Türkiye Finans", 7, false, false, true, null, null),
                new BankSeed("VKF", "VakıfBank", 13, false, false, true, null, null),
                new BankSeed("VKK", "Vakıf Katılım", 9, false, false, true, null, null),
                new BankSeed("ZKT", "Ziraat Katılım", 11, false, false, true, null, null)
            };

            foreach (var seed in bankSeeds)
            {
                var bank = await db.Banks.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.ProviderCode == seed.ProviderCode, ct);

                if (bank == null)
                {
                    db.Banks.Add(new Bank
                    {
                        CompanyId = company.Id,
                        BankName = seed.Name,
                        ProviderCode = seed.ProviderCode,
                        ExternalBankId = seed.ExternalBankId,
                        RequiresLink = seed.RequiresLink,
                        RequiresTLink = seed.RequiresTLink,
                        RequiresAccountNumber = seed.RequiresAccountNumber,
                        DefaultLink = seed.DefaultLink,
                        DefaultTLink = seed.DefaultTLink,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAtUtc = SeedTime
                    });
                }
                else
                {
                    bank.BankName = seed.Name;
                    bank.ExternalBankId = seed.ExternalBankId;
                    bank.RequiresLink = seed.RequiresLink;
                    bank.RequiresTLink = seed.RequiresTLink;
                    bank.RequiresAccountNumber = seed.RequiresAccountNumber;
                    bank.DefaultLink ??= seed.DefaultLink;
                    bank.DefaultTLink ??= seed.DefaultTLink;
                    bank.IsActive = true;
                    bank.IsDeleted = false;
                }
            }

            await db.SaveChangesAsync(ct);

            await DeactivateBankAsync(db, company.Id, "DUMMY", ct);
            await db.SaveChangesAsync(ct);
        }

        private static async Task DeactivateBankAsync(
            FinansDbContext db,
            int companyId,
            string providerCode,
            CancellationToken ct)
        {
            var bank = await db.Banks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.ProviderCode == providerCode, ct);

            if (bank == null)
                return;

            bank.IsActive = false;
            bank.IsDeleted = true;

            var accounts = await db.BankAccounts.IgnoreQueryFilters()
                .Where(x => x.CompanyId == companyId && x.BankId == bank.Id)
                .ToListAsync(ct);

            foreach (var account in accounts)
            {
                account.IsActive = false;
                account.IsDeleted = true;
            }

            var credentials = await db.BankCredentials.IgnoreQueryFilters()
                .Where(x => x.CompanyId == companyId && x.BankId == bank.Id)
                .ToListAsync(ct);

            foreach (var credential in credentials)
            {
                credential.IsActive = false;
                credential.IsDeleted = true;
            }
        }

        private static IReadOnlyList<MenuSeed> MenuSeeds() => new[]
        {
            new MenuSeed("DASHBOARD", "Dashboard", "/Dashboard", "bi-speedometer2", null, 1),
            new MenuSeed("BANK_OPS", "Banka İşlemleri", null, "bi-bank", null, 2),
            new MenuSeed("BANK_TRANSACTIONS", "Hesap Hareketleri", "/Transfer", "bi-arrow-left-right", "BANK_OPS", 1),
            new MenuSeed("BANK_MANAGEMENT", "Banka Tanımları", "/BankManagement/Banks", "bi-building", "BANK_OPS", 2),
            new MenuSeed("ERP_TRANSFER", "ERP Aktarım", null, "bi-box-arrow-right", null, 3),
            new MenuSeed("ERP_BATCH", "Aktarım Kuyrukları", "/ErpTransfer", "bi-list-check", "ERP_TRANSFER", 1),
            new MenuSeed("ERP_FAILED", "Başarısız Aktarımlar", "/ErpTransferFailed", "bi-exclamation-circle", "ERP_TRANSFER", 2),
            new MenuSeed("DEFINITIONS", "Tanımlar", null, "bi-gear", null, 4),
            new MenuSeed("ERP_MAPPING", "ERP Kod Eşleme", "/ErpCodeMapping", "bi-tags", "DEFINITIONS", 1),
            new MenuSeed("RULES", "Kural Motoru", "/BankTransactionRule", "bi-funnel", "DEFINITIONS", 2),
            new MenuSeed("REPORTS", "Raporlar", "/Report", "bi-bar-chart", null, 5),
            new MenuSeed("CONNECTOR", "Connector", "/Connector", "bi-plug", null, 6),
            new MenuSeed("SYSTEM_LOGS", "Sistem Logları", "/SystemLog", "bi-journal-text", null, 7),
            new MenuSeed("USER_MGMT", "Kullanıcı Yönetimi", "/Admin/Users", "bi-people", null, 8)
        };

        private sealed record RoleSeed(string Code, string Name, string Description, bool IsSystemRole);
        private sealed record UserSeed(string UserName, string Email, string FirstName, string LastName, string PasswordHash, bool IsSystemAdmin);
        private sealed record MenuSeed(string Code, string Title, string? Route, string Icon, string? ParentCode, int SortOrder);
        private sealed record BankSeed(string ProviderCode, string Name, int ExternalBankId, bool RequiresLink, bool RequiresTLink, bool RequiresAccountNumber, string? DefaultLink, string? DefaultTLink);
        private sealed record PermissionPreset(bool CanView, bool CanCreate, bool CanUpdate, bool CanDelete, bool CanApprove, bool CanExport)
        {
            public static PermissionPreset Full { get; } = new(true, true, true, true, true, true);
            public static PermissionPreset EditorNoDelete { get; } = new(true, true, true, false, false, true);
            public static PermissionPreset ReadOnlyExport { get; } = new(true, false, false, false, false, true);
        }
    }
}
