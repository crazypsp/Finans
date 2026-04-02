# Finans Yönetim Sistemi

## Proje Hakkında
Firmaların çalıştığı bankalardan hesap hareketlerini çekip, Logo Tiger ERP'ye banka işlem fişi olarak aktaran multi-tenant finans yönetim platformu.

## Mimari
- **Finans.Entities** — Domain entity'leri
- **Finans.Contracts** — DTO'lar
- **Finans.Application** — İş kuralları ve servisler
- **Finans.Data** — EF Core DbContext + Migrations
- **Finans.Infrastructure** — Banka provider'ları, Dapper query'leri
- **Finans.WebMvc** — ASP.NET Core MVC Web Uygulaması
- **Finans.Api** — REST API (genişlemeye hazır)
- **Finans.WorkerService** — Background worker (banka import + ERP transfer)
- **Finans.DesktopConnector** — Logo Tiger COM entegrasyonu (Windows Service)

## Kurulum

### 1. Veritabanı
```bash
# Finans.Data projesinde migration oluştur
cd Finans.Data
dotnet ef migrations add InitialCreate --startup-project ../Finans.WebMvc

# Veritabanını oluştur
dotnet ef database update --startup-project ../Finans.WebMvc
```

### 2. Connection String
`appsettings.json` dosyalarında SQL Server bağlantı bilgisini güncelleyin:
```json
"ConnectionStrings": {
  "FinansDb": "Server=.;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Web Uygulaması
```bash
cd Finans.WebMvc
dotnet run
```

## Kullanıcılar (Seed Data)

| Kullanıcı   | Şifre          | Rol              | Yetkiler                                    |
|-------------|----------------|------------------|---------------------------------------------|
| admin       | Admin123!      | ADMIN            | Tüm menüler ve işlemler                     |
| system      | System123!     | COMPANY_ADMIN    | Dashboard, Banka, ERP Transfer, Raporlar    |
| muhasebe    | Muhasebe123!   | ACCOUNTANT       | Dashboard, Banka, ERP, Tanımlar, Raporlar   |

## Modüller

### Dashboard
- Toplam işlem, aktarılmış/aktarılmamış sayıları
- Chart.js ile bar ve doughnut grafikleri
- Sistem sağlığı (connector, credential, son import/transfer)

### Hesap Hareketleri (/Transfer)
- Banka, tarih, hesap no, açıklama filtresi
- Toplu veya tekli ERP aktarım
- Modal ile GL, Cari, Banka hesap kodu seçimi

### ERP Aktarım (/ErpTransfer)
- Batch bazlı aktarım kuyrukları
- Başarılı/Başarısız sayaçları
- Tekrar deneme (retry) özelliği

### Banka Tanımları (/BankManagement)
- Banka ekleme (provider kodu, link parametreleri)
- Hesap ekleme (IBAN, şube, müşteri no)
- Credential yönetimi

### Tanımlar
- ERP Kod Eşleme: Banka → Muhasebe hesabı eşleme
- Kural Motoru: Açıklama, tutar, döviz bazlı otomatik eşleme

### Raporlar (/Report)
- Banka import raporu
- ERP transfer raporu
- Connector raporu
- Özet istatistikler

### Sistem Logları (/SystemLog)
- Denetim logları (audit trail)
- Sistem bildirimleri (okundu/okunmadı)

## DesktopConnector — Logo Tiger Entegrasyonu

### Kurulum
1. Logo Tiger kurulu olan Windows makinede çalıştırın
2. `appsettings.json` dosyasında Logo Tiger bilgilerini ayarlayın:
```json
"LogoTiger": {
  "UserName": "LOGO_KULLANICI",
  "Password": "LOGO_SIFRE",
  "FirmNr": 1,
  "PeriodNr": 1
}
```

### Çalışma Şekli
- **ConnectorHeartbeatWorker**: 60 sn aralıkla heartbeat gönderir
- **ErpTransferWorker**: 30 sn aralıkla pending transfer item'larını Logo Tiger'a aktarır
- **LogoTigerAccountSyncService**: GL, Cari, Banka hesap planlarını Logo Tiger COM üzerinden senkronize eder
- **LogoTigerTransferService**: Banka hareketlerini Logo Tiger muhasebe fişi olarak oluşturur

### Windows Service olarak kurulum
```bash
sc create FinansConnector binpath="C:\...\Finans.DesktopConnector.exe" start=auto
sc start FinansConnector
```

## Banka Entegrasyonları
Desteklenen bankalar:
- Akbank (AKB)
- İş Bankası (ISB)
- Kuveyt Türk (KTB)
- VakıfBank (VKF)
- Ziraat Katılım (ZKT)
- Türkiye Finans (TFB)
- Vakıf Katılım (VKK)

Her banka provider'ı `IBankProvider` arayüzünü implement eder ve `ProviderCode` ile otomatik keşfedilir.

## Teknolojiler
- .NET 8.0
- ASP.NET Core MVC
- Entity Framework Core 8 (migration/schema)
- Dapper (runtime queries)
- SQL Server
- Bootstrap 5 + Chart.js
- Logo Tiger UnityObjects COM (DesktopConnector)
- Serilog (loglama)
- Polly (retry policies)
