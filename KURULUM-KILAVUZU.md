# Finans Yönetim Sistemi — Kurulum, Yapılandırma ve Dağıtım Kılavuzu

---

## 1. VERİTABANI BAĞLANTI AYARI

SQL Server bağlantı bilginiz görsele göre:
- Server Name: `DESKTOP-54QF28R\ZRV2014EXP`
- Authentication: Windows Authentication

### Değiştirilmesi gereken dosyalar (3 adet):

**Finans.WebMvc/appsettings.json**
```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Finans.WorkerService/appsettings.json**
```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Finans.DesktopConnector/appsettings.json**
```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

> ÖNEMLİ: JSON dosyasında backslash `\` karakteri `\\` olarak yazılır.
> `Trusted_Connection=True` → Windows Authentication kullanır.
> Eğer SQL Authentication kullanmak isterseniz:
> `"Server=DESKTOP-54QF28R\\ZRV2014EXP;Database=FinansDB;User Id=sa;Password=SifreNiz;TrustServerCertificate=True;"`


---

## 2. MİGRATION İŞLEMİ (Veritabanını Ayağa Kaldırma)

### Yöntem A: Visual Studio üzerinden (Package Manager Console)

1. Visual Studio'da solution'ı açın
2. Üstten: **Tools → NuGet Package Manager → Package Manager Console**
3. Console'da **Default project** olarak `Finans.Data` seçin
4. Sırasıyla çalıştırın:

```powershell
# 1) Migration oluştur
Add-Migration InitialCreate -StartupProject Finans.WebMvc

# 2) Veritabanını oluştur (tüm tablolar + seed data)
Update-Database -StartupProject Finans.WebMvc
```

### Yöntem B: Terminal / Komut Satırı

```bash
# Solution klasörüne gidin
cd C:\...\Finans\Finans

# 1) Migration oluştur
dotnet ef migrations add InitialCreate --project Finans.Data --startup-project Finans.WebMvc

# 2) Veritabanını oluştur
dotnet ef database update --project Finans.Data --startup-project Finans.WebMvc
```

### Migration başarılı olduğunda:
- `FinansDB` adında veritabanı oluşur
- Tüm tablolar (Banks, BankTransactions, Users, Roles vs.) oluşur
- 3 kullanıcı seed edilir: `admin`, `system`, `muhasebe`
- 6 rol, 14 menü öğesi, menü yetkileri seed edilir
- Demo firma (Demo Firma A.Ş.) oluşur

### Hata alırsanız:
- EF Core CLI yüklü değilse: `dotnet tool install --global dotnet-ef`
- Connection string'in doğruluğunu kontrol edin
- SQL Server'ın çalıştığından emin olun


---

## 3. LOGO TİGER ENTEGRASYONU — DETAYLI AÇIKLAMA

### 3.1 Mimari: Kim Ne Yapar?

```
┌─────────────────────────────────────────────────────┐
│                    SQL SERVER                        │
│              (FinansDB veritabanı)                   │
│  Tablolar: BankTransactions, ErpTransferItems, ...   │
└──────────┬──────────────────────┬────────────────────┘
           │                      │
           │ (EF Core + Dapper)   │ (EF Core + Dapper)
           │                      │
┌──────────▼──────────┐  ┌───────▼────────────────────┐
│   Finans.WebMvc     │  │  Finans.DesktopConnector   │
│   (Web Uygulaması)  │  │  (Windows Service)         │
│                     │  │                             │
│ • Dashboard         │  │ • LogoTigerTransferService  │
│ • Hesap Hareketleri │  │   → COM ile fiş oluşturur  │
│ • Transfer butonu   │  │                             │
│ • Batch oluşturma   │  │ • LogoTigerAccountSync      │
│                     │  │   → GL/Cari/Banka çeker    │
│ Kullanıcı "Aktar"   │  │                             │
│ butonuna basar →    │  │ • ErpTransferWorker         │
│ ErpTransferItem     │  │   → Pending item'ları alır  │
│ tablosuna "Pending" │  │   → Logo Tiger'a aktarır    │
│ kayıt yazar.        │  │   → Sonucu DB'ye yazar      │
└─────────────────────┘  └───────┬────────────────────┘
                                  │
                                  │ COM / UnityObjects DLL
                                  │
                         ┌────────▼────────┐
                         │   LOGO TIGER    │
                         │  (Muhasebe ERP) │
                         │                 │
                         │ UnityObjects    │
                         │ COM DLL'i       │
                         └─────────────────┘
```

### 3.2 Akış Nasıl Çalışır?

**ADIM 1 — Kullanıcı Web'de işlem seçer:**
- `/Transfer` sayfasında banka hareketlerini filtreler
- Aktarılmamış bir kaydın yanındaki "Aktar" butonuna basar
- Modal açılır → GL kodu, Cari hesap, Banka hesap kodu seçer
- "Aktar" butonuna basar

**ADIM 2 — Web projesi DB'ye yazar:**
- `TransferController.TransferSingle()` çağrılır
- `TransferBatchService.CreateBatchAsync()` çalışır
- `ErpTransferBatch` tablosuna batch kaydı oluşur (Status="Pending")
- `ErpTransferItem` tablosuna item kaydı oluşur (Status="Pending")
- `BankTransaction.TransferBatchNo` güncellenir

**ADIM 3 — DesktopConnector periyodik tarar:**
- `ErpTransferWorker` her 30 saniyede bir çalışır
- `ErpTransferExecutor.ExecutePendingAsync()` çağrılır
- `ErpTransferItem` tablosundan Status="Pending" kayıtları çeker

**ADIM 4 — Logo Tiger'a aktarım:**
- `LogoTigerErpTransferClient.TransferAsync()` çağrılır
- `LogoTigerTransferService.TransferBankTransactionAsync()` çalışır:
  1. `Type.GetTypeFromProgID("UnityObjects.UnityApplication")` → COM nesnesi oluşturur
  2. `unityApp.Login(userName, password, firmNr, periodNr)` → Logo'ya bağlanır
  3. `unityApp.NewDataObject(24)` → GL Voucher (muhasebe fişi) nesnesi oluşturur
  4. Header bilgilerini doldurur (tarih, belge no, açıklama)
  5. Satırları doldurur:
     - Alacak (C) ise: Banka hesabı BORÇ, GL hesabı ALACAK
     - Borç (D) ise: GL hesabı BORÇ, Banka hesabı ALACAK
  6. `data.Post()` → Logo Tiger'a kaydeder
  7. Voucher numarasını alır

**ADIM 5 — Sonuç DB'ye yazılır:**
- Başarılı: `ErpTransferItem.Status = "Success"`, `BankTransaction.IsTransferred = true`
- Başarısız: `ErpTransferItem.Status = "Failed"`, hata mesajı kaydedilir
- `ErpTransferLog` tablosuna log yazılır
- Başarısız ise `SystemNotification` oluşturulur

### 3.3 Logo Tiger Hesap Planı Çekme

`LogoTigerAccountSyncService` COM üzerinden:
- **GL Hesapları**: `doAccountPlan = 12` → `ErpGlAccounts` tablosuna
- **Cari Hesaplar**: `doArpCard = 18` → `ErpCurrentAccounts` tablosuna
- **Banka Hesapları**: `doBankAccount = 62` → `ErpBankAccounts` tablosuna

Web'den tetiklenir: Sol menüde "ERP Senkronize" bağlantısına tıklanınca
`ErpSyncController.SyncAll()` → `ErpAccountSyncService` çağrılır.

> ÖNEMLİ: Web projesi üzerinden senkronizasyon yapıldığında,
> Application katmanındaki `ErpAccountSyncService` çalışır
> (Logo Tiger SQL veritabanına direkt bağlanır).
>
> DesktopConnector üzerinden yapıldığında,
> `LogoTigerAccountSyncService` çalışır
> (Logo Tiger COM DLL'i ile hesap planlarını çeker).

### 3.4 Logo Tiger için Gerekli Ön Koşullar

1. Logo Tiger kurulu olmalı (aynı makinede veya erişilebilir)
2. `UnityObjects.dll` COM olarak kayıtlı olmalı:
   ```
   regsvr32 "C:\Logo\Tiger3\UnityObjects.dll"
   ```
3. `appsettings.json` ayarları:
   ```json
   "LogoTiger": {
     "UserName": "LOGO_KULLANICI_ADI",
     "Password": "LOGO_SIFRESI",
     "FirmNr": 1,
     "PeriodNr": 1
   }
   ```
   - `FirmNr`: Logo Tiger'daki firma numarası (genellikle 1)
   - `PeriodNr`: Çalışma dönemi numarası (genellikle 1)

### 3.5 UnityObjects DataObject Türleri (Referans)

| Sabit | Değer | Açıklama |
|-------|-------|----------|
| doAccountPlan | 12 | Muhasebe Hesap Planı |
| doArpCard | 18 | Cari Hesap Kartları |
| doGLVoucher | 24 | Muhasebe Fişi (Banka İşlem Fişi) |
| doBankAccount | 62 | Banka Hesap Kartları |


---

## 4. HANGİ PROJELERİ ÇALIŞTIRMALI?

### Geliştirme Ortamında (Debug):

| Proje | Ne Yapar | Ne Zaman Çalıştırılır |
|-------|----------|----------------------|
| **Finans.WebMvc** | Web arayüzü | HER ZAMAN |
| **Finans.DesktopConnector** | Logo Tiger aktarımı | Logo Tiger varsa |
| **Finans.WorkerService** | Otomatik banka import | Banka entegrasyonu test ederken |

### Minimum çalıştırma:
- Sadece **Finans.WebMvc** — Web arayüzü, elle aktarım batch oluşturma

### Tam sistem:
- **Finans.WebMvc** + **Finans.DesktopConnector** — Web + Logo Tiger aktarımı
- (Opsiyonel) **Finans.WorkerService** — Otomatik banka import

### Visual Studio'da birden fazla proje çalıştırma:
1. Solution Explorer'da solution'a sağ tık
2. "Set Startup Projects..." seçin
3. "Multiple startup projects" seçin
4. Finans.WebMvc → Start
5. Finans.DesktopConnector → Start
6. OK'a basın
7. F5 ile hepsini başlatın


---

## 5. DEBUG NASIL YAPILIR?

### 5.1 Web Projesi Debug

1. Visual Studio'da `Finans.WebMvc`'yi startup project olarak seçin
2. F5'e basın (veya Debug → Start Debugging)
3. Tarayıcı açılır → Login ekranı gelir
4. `admin` / `Admin123!` ile giriş yapın

**Breakpoint koyma:**
- Herhangi bir Controller veya Service dosyasında sol kenar boşluğuna tıklayın
- Örnek: `TransferController.cs` → `CreateBatch` metoduna breakpoint koyun
- Web'de "Aktar" butonuna basınca breakpoint'e düşer

### 5.2 DesktopConnector Debug

1. Solution Explorer → Finans.DesktopConnector'a sağ tık → "Set as Startup Project"
2. F5 ile başlatın (Console uygulaması olarak çalışır)
3. `LogoTigerTransferService.cs` → `TransferBankTransactionAsync` metoduna breakpoint koyun
4. Web'den bir aktarım oluşturun (Status=Pending)
5. 30 saniye bekleyin → Worker tetiklenir → Breakpoint'e düşer

### 5.3 Faydalı Debug İpuçları

**Output Window'u kullanın:**
- Visual Studio → View → Output
- "Show output from" → ASP.NET Core Web Server
- Log mesajlarını görebilirsiniz

**SQL Server Profiler:**
- SQL Server Management Studio → Tools → SQL Server Profiler
- Hangi SQL sorgularının çalıştığını görebilirsiniz

**Serilog logları (DesktopConnector):**
- `logs/` klasöründe günlük log dosyaları oluşur
- `connector-20260330.log` gibi dosyalar


---

## 6. SUNUCUYA KURULUM (Production Deployment)

### 6.1 Genel Mimari

```
┌─────────────────────────────────────────────┐
│              SUNUCU (Windows Server)         │
│                                             │
│  ┌────────────────────┐                     │
│  │    IIS / Kestrel   │ ← Port 80/443      │
│  │   Finans.WebMvc    │   (dış erişim)      │
│  └────────┬───────────┘                     │
│           │                                 │
│           ▼                                 │
│  ┌────────────────────┐                     │
│  │    SQL Server      │                     │
│  │    FinansDB        │                     │
│  └────────┬───────────┘                     │
│           │                                 │
│  ┌────────▼───────────┐                     │
│  │ Windows Service    │                     │
│  │ DesktopConnector   │ ← Logo Tiger COM    │
│  └────────────────────┘                     │
│                                             │
│  ┌────────────────────┐ (opsiyonel)         │
│  │ Windows Service    │                     │
│  │ WorkerService      │ ← Banka import      │
│  └────────────────────┘                     │
└─────────────────────────────────────────────┘
```

### 6.2 Web Projesi Kurulumu (IIS)

**Adım 1: Publish**
```bash
cd Finans.WebMvc
dotnet publish -c Release -o C:\inetpub\wwwroot\Finans
```
Veya Visual Studio'da: Sağ tık → Publish → Folder → Publish

**Adım 2: IIS Ayarı**
1. IIS Manager açın
2. Sites → Add Website
   - Site name: `Finans`
   - Physical path: `C:\inetpub\wwwroot\Finans`
   - Port: `80` (veya istediğiniz port)
3. Application Pool → "Finans" pool → Advanced Settings
   - .NET CLR Version: `No Managed Code`
   - Identity: `LocalSystem` veya özel bir kullanıcı

**Adım 3: .NET Hosting Bundle**
Sunucuya .NET 8 Hosting Bundle yüklenmeli:
https://dotnet.microsoft.com/download/dotnet/8.0 → Hosting Bundle

**Adım 4: appsettings.json**
Sunucudaki bağlantı bilgisini güncelleyin:
```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=SUNUCU_ADI\\INSTANCE;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 6.3 DesktopConnector Kurulumu (Windows Service)

**Adım 1: Publish**
```bash
cd Finans.DesktopConnector
dotnet publish -c Release -o C:\Services\FinansConnector
```

**Adım 2: appsettings.json düzenle**
```json
{
  "ConnectionStrings": {
    "FinansDb": "Server=SUNUCU_ADI\\INSTANCE;Database=FinansDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "LogoTiger": {
    "UserName": "GERCEK_LOGO_KULLANICI",
    "Password": "GERCEK_LOGO_SIFRE",
    "FirmNr": 1,
    "PeriodNr": 1
  }
}
```

**Adım 3: Windows Service olarak kur**
Komut satırını YÖNETİCİ olarak açın:
```cmd
sc create FinansConnector binpath="C:\Services\FinansConnector\Finans.DesktopConnector.exe" start=auto displayname="Finans Desktop Connector"
sc description FinansConnector "Logo Tiger ERP entegrasyonu ve heartbeat servisi"
sc start FinansConnector
```

**Service'i kaldırmak için:**
```cmd
sc stop FinansConnector
sc delete FinansConnector
```

### 6.4 WorkerService Kurulumu (Windows Service)

```bash
cd Finans.WorkerService
dotnet publish -c Release -o C:\Services\FinansWorker
```

```cmd
sc create FinansWorker binpath="C:\Services\FinansWorker\Finans.WorkerService.exe" start=auto displayname="Finans Worker Service"
sc start FinansWorker
```

### 6.5 Logo Tiger'ın Farklı Makinede Olması Durumu

Logo Tiger başka bir makinede kurulu ise:
- **DesktopConnector** Logo Tiger'ın olduğu makinede çalışmalı
- `appsettings.json`'daki SQL Server bağlantısı uzak sunucuyu göstermeli
- Logo Tiger COM DLL'i sadece yerel makineye erişir

```
┌──────────────────┐          ┌──────────────────┐
│   WEB SUNUCUSU   │          │  LOGO MAKİNESİ   │
│                  │          │                  │
│  Finans.WebMvc   │          │ DesktopConnector │
│  WorkerService   │          │ Logo Tiger       │
│                  │          │ UnityObjects.dll │
│    SQL Server ◄──┼──────────┼─ (uzak bağlantı) │
└──────────────────┘          └──────────────────┘
```


---

## 7. PROJELER ARASI İLETİŞİM

### 7.1 İletişim Yöntemi: PAYLAŞILAN VERİTABANI

Projeler birbirleriyle **doğrudan iletişim kurmaz**. Tüm iletişim **SQL Server veritabanı üzerinden** gerçekleşir.

```
Web Projesi                   DesktopConnector
     │                              │
     │ INSERT INTO ErpTransferItem  │
     │ (Status='Pending')           │
     │         ↓                    │
     │    ┌─────────┐               │
     │    │ SQL DB  │               │
     │    └─────────┘               │
     │         ↑                    │
     │                SELECT FROM   │
     │                ErpTransferItem│
     │                WHERE Status  │
     │                = 'Pending'   │
     │                              │
     │                UPDATE Status │
     │                = 'Success'   │
```

### 7.2 Veri Akışı Tabloları

| Tablo | Kim Yazar | Kim Okur | Amaç |
|-------|-----------|----------|------|
| ErpTransferBatch | WebMvc | DesktopConnector | Aktarım kuyruğu |
| ErpTransferItem | WebMvc (oluşturur), Connector (günceller) | Her ikisi | Transfer detayları |
| BankTransaction | WorkerService (import), Connector (günceller) | WebMvc | Banka hareketleri |
| DesktopConnectorClient | DesktopConnector | WebMvc | Connector durumu |
| DesktopConnectorHeartbeatLog | DesktopConnector | WebMvc | Heartbeat logları |
| ErpGlAccount | ErpAccountSync | WebMvc | GL hesap planı |
| ErpCurrentAccount | ErpAccountSync | WebMvc | Cari hesaplar |
| ErpBankAccount | ErpAccountSync | WebMvc | Banka hesap kodları |
| SystemNotification | Connector | WebMvc | Hata bildirimleri |

### 7.3 Neden API Değil de Veritabanı?

- **Basitlik**: HTTP API gerektirmez, firewall kuralı gerekmez
- **Güvenilirlik**: DB işlem garantisi (transaction)
- **Asenkron**: Worker pattern ile decoupled çalışma
- **Offline tolerans**: Connector kapalı olsa bile web çalışır, kuyruk birikir
- **Logo Tiger kısıtı**: COM DLL yalnızca yerel çalışır, HTTP üzerinden erişilemez


---

## 8. ÖZET: İLK ÇALIŞTIRMA ADIMLARI

```
1. appsettings.json dosyalarında connection string'i değiştir
   → 3 dosya: WebMvc, WorkerService, DesktopConnector

2. Migration çalıştır
   → Package Manager Console: Add-Migration InitialCreate -StartupProject Finans.WebMvc
   → Package Manager Console: Update-Database -StartupProject Finans.WebMvc

3. Finans.WebMvc'yi çalıştır (F5)
   → Tarayıcıda Login ekranı gelir
   → admin / Admin123! ile giriş yap

4. Logo Tiger varsa: Finans.DesktopConnector'ı çalıştır
   → appsettings.json'da Logo bilgilerini gir
   → Console'da heartbeat loglarını gör

5. Banka entegrasyonu test etmek için:
   → BankManagement'tan banka + hesap + credential ekle
   → WorkerService çalıştır (otomatik import)
   → veya DummyBankProvider ile test et
```
