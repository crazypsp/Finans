using Finans.Entities.Banking;
using Finans.Entities.Common;
using Finans.Entities.Connector;
using Finans.Entities.ERP;
using Finans.Entities.Identity;
using Finans.Entities.Integration;
using Finans.Entities.Integrations;
using Finans.Entities.Localization;
using Finans.Entities.Logging;
using Finans.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Finans.Data.Context
{
    /// <summary>
    /// Neden var?
    /// - EF Core'u burada sadece "schema + migration" için kullanıyoruz.
    /// - Runtime query'lerde (dashboard, listeleme, rapor) Dapper kullanacağız.
    /// - Böylece schema yönetimi kontrollü, runtime performanslı olur.
    /// </summary>
    public class FinansDbContext : DbContext
    {
        public FinansDbContext(DbContextOptions<FinansDbContext> options) : base(options) { }

        // Tenancy
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDomain> CompanyDomains => Set<CompanyDomain>();
        public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
        public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();

        // Identity
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<RoleMenuPermission> RoleMenuPermissions => Set<RoleMenuPermission>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        // Localization
        public DbSet<Culture> Cultures => Set<Culture>();
        public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
        public DbSet<LocalizationResourceTranslation> LocalizationResourceTranslations => Set<LocalizationResourceTranslation>();

        // Banking
        public DbSet<Bank> Banks => Set<Bank>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
        public DbSet<BankCredential> BankCredentials => Set<BankCredential>();
        public DbSet<BankApiPayload> BankApiPayloads => Set<BankApiPayload>();
        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
        public DbSet<TransactionImport> TransactionImports => Set<TransactionImport>();

        // ERP
        public DbSet<ErpSystem> ErpSystems => Set<ErpSystem>();
        public DbSet<CompanyErpConnection> CompanyErpConnections => Set<CompanyErpConnection>();
        public DbSet<ErpQueryTemplate> ErpQueryTemplates => Set<ErpQueryTemplate>();
        public DbSet<ErpCurrentAccount> ErpCurrentAccounts => Set<ErpCurrentAccount>();
        public DbSet<ErpGlAccount> ErpGlAccounts => Set<ErpGlAccount>();
        public DbSet<ErpBankAccount> ErpBankAccounts => Set<ErpBankAccount>();
        public DbSet<ErpTransferBatch> ErpTransferBatches => Set<ErpTransferBatch>();
        public DbSet<ErpTransferItem> ErpTransferItems => Set<ErpTransferItem>();
        public DbSet<ErpCodeMapping> ErpCodeMappings => Set<ErpCodeMapping>();
        public DbSet<BankTransactionRule> BankTransactionRules => Set<BankTransactionRule>();


        // Integrations
        public DbSet<IntegrationEndpoint> IntegrationEndpoints => Set<IntegrationEndpoint>();
        public DbSet<IntegrationCredential> IntegrationCredentials => Set<IntegrationCredential>();

        // Connector
        public DbSet<DesktopConnectorInstallation> DesktopConnectorInstallations => Set<DesktopConnectorInstallation>();
        public DbSet<DesktopConnectorHeartbeat> DesktopConnectorHeartbeats => Set<DesktopConnectorHeartbeat>();
        public DbSet<DesktopConnectorLicense> DesktopConnectorLicenses => Set<DesktopConnectorLicense>();

        // Logging
        public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
        public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
        public DbSet<BankIntegrationLog> BankIntegrationLogs => Set<BankIntegrationLog>();
        public DbSet<ErpTransferLog> ErpTransferLogs => Set<ErpTransferLog>();
        public DbSet<ExportLog> ExportLogs => Set<ExportLog>();
        public DbSet<MatchingLog> MatchingLogs => Set<MatchingLog>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SystemNotification> SystemNotifications => Set<SystemNotification>();

        //Destop Connector
        public DbSet<DesktopConnectorClient> DesktopConnectorClients => Set<DesktopConnectorClient>();
        public DbSet<DesktopConnectorHeartbeatLog> DesktopConnectorHeartbeatLogs => Set<DesktopConnectorHeartbeatLog>();
        public DbSet<DesktopConnectorLicenseLog> DesktopConnectorLicenseLogs => Set<DesktopConnectorLicenseLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Concurrency:
            // Neden var?
            // - Aynı bank transaction satırına birden fazla kullanıcı/worker aynı anda dokunabilir.
            // - RowVersion ile concurrency conflict yakalarız.
            modelBuilder.Entity<User>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Role>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Company>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<BankTransaction>().Property(x => x.RowVersion).IsRowVersion();

            // Unique Index:
            // Neden var?
            // - Çift kayıtları önlemek ve kuralları DB seviyesinde garantiye almak.
            modelBuilder.Entity<Role>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<MenuItem>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Company>().HasIndex(x => x.PrimaryDomain).IsUnique();
            modelBuilder.Entity<BankTransaction>().HasIndex(x => x.ExternalUniqueKey).IsUnique();

            // Soft Delete filter (minimum set):
            // Neden var?
            // - Fiziksel silme yerine işaretleyip veri kaybını önlemek.
            modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Role>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Company>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<BankTransaction>().HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<ErpTransferItem>()
    .HasIndex(x => x.BankTransactionId)
    .IsUnique();

            SeedIdentity(modelBuilder);
            SeedMenuItems(modelBuilder);
        }

        private static void SeedIdentity(ModelBuilder modelBuilder)
        {
            var seedTime = new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc);
            // Tüm şifreler aynı deterministik salt ile üretildi (PBKDF2-SHA256, 100K iterasyon)
            const string seedSalt = "AQIDBAUGBwgJCgsMDQ4PEA==";

            // --- ROLLER ---
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Sistem Yöneticisi", Code = RoleCodes.ADMIN, Description = "Tüm yetkilere sahip sistem yöneticisi", IsSystemRole = true, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new Role { Id = 2, Name = "Bayi", Code = RoleCodes.DEALER, Description = "Bayi kullanıcısı", IsSystemRole = false, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new Role { Id = 3, Name = "Alt Bayi", Code = RoleCodes.SUB_DEALER, Description = "Alt bayi kullanıcısı", IsSystemRole = false, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new Role { Id = 4, Name = "Muhasebeci", Code = RoleCodes.ACCOUNTANT, Description = "Muhasebe firması / SMMM", IsSystemRole = false, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new Role { Id = 5, Name = "Firma Yöneticisi", Code = RoleCodes.COMPANY_ADMIN, Description = "Firma yöneticisi", IsSystemRole = false, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new Role { Id = 6, Name = "Firma Kullanıcısı", Code = RoleCodes.COMPANY_USER, Description = "Firma standart kullanıcı", IsSystemRole = false, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime }
            );

            // --- KULLANICILAR ---
            // Admin: admin / Admin123!
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    UserName = "admin",
                    Email = "admin@finans.local",
                    FirstName = "Sistem",
                    LastName = "Yönetici",
                    PasswordHash = "HcXuAK10SjWLJPp2fjPLGDZzYBUHjuLTj/VUPgmfEuE=",
                    PasswordSalt = seedSalt,
                    IsActive = true,
                    IsEmailVerified = true,
                    IsSystemAdmin = true,
                    IsDeleted = false,
                    CreatedAtUtc = seedTime
                },
                // Sistem: system / System123!
                new User
                {
                    Id = 2,
                    UserName = "system",
                    Email = "system@finans.local",
                    FirstName = "Sistem",
                    LastName = "Servisi",
                    PasswordHash = "tZ6GcKBQNHdPy4lKFkUbrqEP7oFIotGX4NTy/o1qTYE=",
                    PasswordSalt = seedSalt,
                    IsActive = true,
                    IsEmailVerified = true,
                    IsSystemAdmin = false,
                    IsDeleted = false,
                    CreatedAtUtc = seedTime
                },
                // Muhasebe: muhasebe / Muhasebe123!
                new User
                {
                    Id = 3,
                    UserName = "muhasebe",
                    Email = "muhasebe@finans.local",
                    FirstName = "Muhasebe",
                    LastName = "Kullanıcı",
                    PasswordHash = "7kRWXzTNa95r4b/Yz/Gs764Xa+vhx28hJOiAH7Ul4k8=",
                    PasswordSalt = seedSalt,
                    IsActive = true,
                    IsEmailVerified = true,
                    IsSystemAdmin = false,
                    IsDeleted = false,
                    CreatedAtUtc = seedTime
                }
            );

            // --- KULLANICI ROL ATAMALARI ---
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { Id = 1, UserId = 1, RoleId = 1, ScopeCompanyId = null, AssignedAtUtc = seedTime, AssignedByUserId = 1, IsDeleted = false, CreatedAtUtc = seedTime },
                new UserRole { Id = 2, UserId = 2, RoleId = 5, ScopeCompanyId = 1, AssignedAtUtc = seedTime, AssignedByUserId = 1, IsDeleted = false, CreatedAtUtc = seedTime },
                new UserRole { Id = 3, UserId = 3, RoleId = 4, ScopeCompanyId = 1, AssignedAtUtc = seedTime, AssignedByUserId = 1, IsDeleted = false, CreatedAtUtc = seedTime }
            );

            // --- FİRMA ---
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    Id = 1,
                    Name = "Demo Firma A.Ş.",
                    CompanyType = CompanyType.ClientCompany,
                    ParentCompanyId = null,
                    PrimaryDomain = "demo.finans.local",
                    CountryIso2 = "TR",
                    DefaultCultureName = "tr-TR",
                    TimeZoneId = "Europe/Istanbul",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAtUtc = seedTime
                }
            );

            // --- FİRMA KULLANICILARI ---
            modelBuilder.Entity<CompanyUser>().HasData(
                new CompanyUser { Id = 1, CompanyId = 1, UserId = 1, IsCompanyAdmin = true, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new CompanyUser { Id = 2, CompanyId = 1, UserId = 2, IsCompanyAdmin = true, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new CompanyUser { Id = 3, CompanyId = 1, UserId = 3, IsCompanyAdmin = false, IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime }
            );

            // --- ERP SİSTEMİ ---
            modelBuilder.Entity<ErpSystem>().HasData(
                new ErpSystem { Id = 1, Name = "Logo Tiger", Code = "LOGO_TIGER", IsActive = true, IsDeleted = false, CreatedAtUtc = seedTime }
            );
        }

        private static void SeedMenuItems(ModelBuilder modelBuilder)
        {
            var seedTime = new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem { Id = 1,  Title = "Dashboard",           Code = "DASHBOARD",          Route = "/Dashboard",              Icon = "bi-speedometer2",      ParentMenuItemId = null, SortOrder = 1,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 2,  Title = "Banka İşlemleri",     Code = "BANK_OPS",           Route = null,                      Icon = "bi-bank",              ParentMenuItemId = null, SortOrder = 2,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 3,  Title = "Hesap Hareketleri",   Code = "BANK_TRANSACTIONS",  Route = "/Transfer",               Icon = "bi-arrow-left-right",  ParentMenuItemId = 2,    SortOrder = 1,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 4,  Title = "Banka Tanımları",     Code = "BANK_MANAGEMENT",    Route = "/BankManagement/Banks",   Icon = "bi-building",          ParentMenuItemId = 2,    SortOrder = 2,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 5,  Title = "ERP Aktarım",         Code = "ERP_TRANSFER",       Route = null,                      Icon = "bi-box-arrow-right",   ParentMenuItemId = null, SortOrder = 3,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 6,  Title = "Aktarım Kuyrukları",  Code = "ERP_BATCH",          Route = "/ErpTransfer",            Icon = "bi-list-check",        ParentMenuItemId = 5,    SortOrder = 1,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 7,  Title = "Başarısız Aktarımlar", Code = "ERP_FAILED",        Route = "/ErpTransferFailed",      Icon = "bi-exclamation-circle", ParentMenuItemId = 5,   SortOrder = 2,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 8,  Title = "Tanımlar",            Code = "DEFINITIONS",        Route = null,                      Icon = "bi-gear",              ParentMenuItemId = null, SortOrder = 4,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 9,  Title = "ERP Kod Eşleme",     Code = "ERP_MAPPING",        Route = "/ErpCodeMapping",         Icon = "bi-tags",              ParentMenuItemId = 8,    SortOrder = 1,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 10, Title = "Kural Motoru",        Code = "RULES",              Route = "/BankTransactionRule",    Icon = "bi-funnel",            ParentMenuItemId = 8,    SortOrder = 2,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 11, Title = "Raporlar",            Code = "REPORTS",            Route = "/Report",                 Icon = "bi-bar-chart",         ParentMenuItemId = null, SortOrder = 5,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 12, Title = "Connector",           Code = "CONNECTOR",          Route = "/Connector",              Icon = "bi-plug",              ParentMenuItemId = null, SortOrder = 6,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 13, Title = "Sistem Logları",      Code = "SYSTEM_LOGS",        Route = "/SystemLog",              Icon = "bi-journal-text",      ParentMenuItemId = null, SortOrder = 7,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime },
                new MenuItem { Id = 14, Title = "Kullanıcı Yönetimi",  Code = "USER_MGMT",          Route = "/Admin/Users",            Icon = "bi-people",            ParentMenuItemId = null, SortOrder = 8,  IsVisible = true, IsDeleted = false, CreatedAtUtc = seedTime }
            );

            // --- ROL MENÜ YETKİLERİ ---
            // ADMIN: tüm menülere tam yetki
            var roleMenuId = 1;
            for (var menuId = 1; menuId <= 14; menuId++)
            {
                modelBuilder.Entity<RoleMenuPermission>().HasData(
                    new RoleMenuPermission { Id = roleMenuId++, RoleId = 1, MenuItemId = menuId, CanView = true, CanCreate = true, CanUpdate = true, CanDelete = true, CanApprove = true, CanExport = true, IsDeleted = false, CreatedAtUtc = seedTime }
                );
            }

            // ACCOUNTANT: Dashboard, Bank Ops, ERP Transfer, Tanımlar, Raporlar, Sistem Logları
            foreach (var menuId in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13 })
            {
                modelBuilder.Entity<RoleMenuPermission>().HasData(
                    new RoleMenuPermission { Id = roleMenuId++, RoleId = 4, MenuItemId = menuId, CanView = true, CanCreate = true, CanUpdate = true, CanDelete = false, CanApprove = false, CanExport = true, IsDeleted = false, CreatedAtUtc = seedTime }
                );
            }

            // COMPANY_ADMIN: Dashboard, Bank Ops, ERP Transfer, Raporlar
            foreach (var menuId in new[] { 1, 2, 3, 5, 6, 7, 11 })
            {
                modelBuilder.Entity<RoleMenuPermission>().HasData(
                    new RoleMenuPermission { Id = roleMenuId++, RoleId = 5, MenuItemId = menuId, CanView = true, CanCreate = true, CanUpdate = true, CanDelete = false, CanApprove = false, CanExport = true, IsDeleted = false, CreatedAtUtc = seedTime }
                );
            }

            // COMPANY_USER: Dashboard, Bank Ops (sadece view), Raporlar (sadece view)
            foreach (var menuId in new[] { 1, 2, 3, 11 })
            {
                modelBuilder.Entity<RoleMenuPermission>().HasData(
                    new RoleMenuPermission { Id = roleMenuId++, RoleId = 6, MenuItemId = menuId, CanView = true, CanCreate = false, CanUpdate = false, CanDelete = false, CanApprove = false, CanExport = true, IsDeleted = false, CreatedAtUtc = seedTime }
                );
            }

            // DEALER: Dashboard, Connector, Raporlar
            foreach (var menuId in new[] { 1, 11, 12 })
            {
                modelBuilder.Entity<RoleMenuPermission>().HasData(
                    new RoleMenuPermission { Id = roleMenuId++, RoleId = 2, MenuItemId = menuId, CanView = true, CanCreate = false, CanUpdate = false, CanDelete = false, CanApprove = false, CanExport = true, IsDeleted = false, CreatedAtUtc = seedTime }
                );
            }
        }
    }
}