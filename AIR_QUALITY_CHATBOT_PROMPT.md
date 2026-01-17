# 🌬️ Hava Kalitesi Chatbot Projesi - Detaylı Geliştirme Prompt'u

## 📋 Proje Özeti

**AirQualityChatBot**, hava kalitesi ölçüm verilerini analiz eden ve kullanıcılara hava kalitesi bilgileri sunan, ASP.NET Core 8.0 Blazor Server tabanlı bir AI chatbot uygulamasıdır. Kullanıcılar hava kalitesi sorguları yapabilir, istatistikler görüntüleyebilir, zaman aralığı analizleri isteyebilir ve özel raporlar alabilir.

---

## ✅ TODO: Implementasyon Planı

### Faz 1: Temel Altyapı ve Veritabanı (1-2 gün)
- [x] **1.1** PostgreSQL veritabanı şemasını oluştur (Chat & Admin tabloları)
- [x] **1.2** PostgreSQL `air_quality_index` tablosunu oluştur (device_id bazlı)
- [x] **1.3** MongoDB bağlantısını yapılandır (`airqoonBaseMapDB`)
- [x] **1.4** Qdrant Docker container'ını başlat ve yapılandır
- [x] **1.5** Entity Framework migration'larını oluştur
- [x] **1.6** `ApplicationDbContext`'i güncelle (yeni entity'ler ile)
- [x] **1.7** `DomainTenantMappings` tablosunu oluştur

### Faz 2: Model ve Entity Güncellemeleri (1 gün)
- [x] **2.1** `ConversationContext` model'ine tenant field'ları ekle
- [x] **2.2** `ConversationContextEntity`'ye tenant field'ları ekle
- [x] **2.3** `ChatSession` entity'sine `TenantSlug` field'ı ekle
- [x] **2.4** Migration oluştur ve uygula
- [x] **2.5** DTO'ları oluştur (`TimeRangeAnalysisResult`, `MonthlyComparisonResult`, vb.)

### Faz 3: Servis Katmanı - Temel Servisler (2-3 gün)
- [x] **3.1** `ITenantMappingService` ve `TenantMappingService` oluştur
- [x] **3.2** `IMongoDbService` ve `MongoDbService` oluştur (Tenant & Device lookup)
- [x] **3.3** `IPostgresAirQualityService` ve `PostgresAirQualityService` oluştur
- [x] **3.4** `IVectorDbService` ve `VectorDbService` oluştur (Qdrant wrapper)
- [x] **3.5** `IMcpClientService` ve `McpClientService` oluştur (MCP protocol)
- [x] **3.6** `IAirQualityMcpService` ve `AirQualityMcpService` oluştur

### Faz 4: MCP Entegrasyonu (1-2 gün)
- [x] **4.1** Python MCP server'ı test et ve çalıştır
- [x] **4.2** MCP client implementasyonunu tamamla (stdio veya HTTP)
- [x] **4.3** Tüm MCP tool'ları test et:
  - `tenant_time_range_analysis`
  - `tenant_monthly_comparison`
  - `tenant_device_list`
  - `tenant_statistics`
  - `save_analysis_to_vector_db`
  - `search_analysis_from_vector_db`

### Faz 5: ChatOrchestrationService Güncellemeleri (2-3 gün)
- [x] **5.1** Intent detection prompt'unu güncelle (tenant bazlı)
- [x] **5.2** `HandleAirQualityQuery` metodunu implement et
- [x] **5.3** `HandleStatisticalAnalysis` metodunu implement et
- [x] **5.4** `HandleMonthlyComparison` metodunu implement et
- [x] **5.5** `ExtractTenantSlug` helper metodunu implement et
- [x] **5.6** `NormalizeTenantSlug` ve `ConvertTenantNameToSlug` metodlarını ekle
- [x] **5.7** Domain'den tenant mapping'i `EnsureSessionAsync`'e entegre et
- [x] **5.8** Context'e tenant bilgisini otomatik kaydet

### Faz 6: LLM Service Güncellemeleri (1 gün)
- [x] **6.1** Intent detection prompt'unu tenant bazlı güncelle
- [x] **6.2** Parametre normalizasyonunu ekle (PM10 → PM10-24h, vb.)
- [x] **6.3** Tenant name -> slug conversion logic'i ekle

### Faz 7: UI Bileşenleri (2-3 gün)
- [x] **7.1** `ChatWidget.razor`'ı güncelle (tenant bilgisi gösterimi)
- [x] **7.2** `AirQualityCard.razor` component'ini oluştur
- [x] **7.3** `AirQualityChart.razor` component'ini oluştur
- [x] **7.4** Chart.js entegrasyonunu yap
- [x] **7.5** Admin dashboard'a tenant yönetimi ekle
- [x] **7.6** Domain -> Tenant mapping UI'ını ekle

### Faz 8: Admin Dashboard Güncellemeleri (1-2 gün)
- [x] **8.1** Tenant listesi görüntüleme (MongoDB'den)
- [x] **8.2** Tenant detay sayfası (cihaz listesi, istatistikler)
- [x] **8.3** Domain -> Tenant mapping yönetimi
- [x] **8.4** Analytics'i tenant bazlı filtreleme ile güncelle

### Faz 9: RAG ve Vector DB (1-2 gün)
- [x] **9.1** Vector DB'ye analiz kaydetme akışını test et
- [x] **9.2** RAG ile context enrichment implementasyonu
- [x] **9.3** Semantic search testleri

### Faz 10: Testing ve Optimizasyon (2-3 gün)
- [x] **10.1** Unit testler (servisler için)
- [x] **10.2** Integration testler (MCP entegrasyonu)
- [x] **10.3** End-to-end testler (chat flow)
- [x] **10.4** Performance optimizasyonu (caching, indexing)
- [x] **10.5** Error handling iyileştirmeleri

### Faz 11: Deployment (1 gün)
- [x] **11.1** Docker Compose yapılandırmasını güncelle
- [x] **11.2** Environment variables yapılandırması
- [x] **11.3** Production deployment hazırlığı

---

## 🚀 Hızlı Başlangıç (İlk Adımlar)

1. **Veritabanı Kurulumu:**
   ```bash
   # PostgreSQL migration
   dotnet ef migrations add InitialAirQualitySchema
   dotnet ef database update
   
   # Qdrant container
   docker-compose up -d qdrant
   
   # MongoDB bağlantısını test et
   ```

2. **MCP Server Test:**
   ```bash
   cd /path/to/Airqoon
   python3 -m venv venv
   source venv/bin/activate
   pip install -r requirements.txt
   python3 vector_db_setup.py
   python3 -c "from mcp_server import *; print('MCP Server OK')"
   ```

3. **İlk Servis Implementasyonu:**
   - `TenantMappingService` ile başla (en basit)
   - Sonra `MongoDbService` (tenant lookup)
   - Sonra `PostgresAirQualityService` (veri çekme)

---

---

## 🎯 Ana Fonksiyonlar

### 1. Hava Kalitesi Sorgulama (AirQualityQuery)
Kullanıcılardan tenant (kurum/şirket) ve zaman bilgileri alarak hava kalitesi verileri sunar:
- **Tenant**: Tenant slug (örn: "akcansa", "tupras", "bursa-metropolitan-municipality")
- **Zaman Aralığı**: Tarih aralığı (başlangıç-bitiş) veya tek tarih
- **Kirletici Türü**: PM2.5, PM10, NO2, SO2, CO, O3, vb. (normalize edilmiş: PM10-24h, PM2.5-24h, NO2-1h)
- **Analiz Tipi**: Anlık değer, ortalama, maksimum, minimum, trend analizi
- **Device-based**: Tenant'a ait tüm cihazların verileri toplanır

### 2. İstatistiksel Analiz (StatisticalAnalysis)
- **Zaman Serisi Analizi**: Tenant bazlı belirli bir zaman aralığındaki değişimler
- **Karşılaştırmalı Analiz**: İki zaman aralığı karşılaştırması (comparison_start_date, comparison_end_date)
- **Aylık Karşılaştırma**: İki ay arasındaki dramatik değişiklikler (%20+ değişim vurgulanır)
- **Trend Analizi**: Artış/azalış trendleri
- **Device Aggregation**: Tenant'a ait tüm cihazların verileri toplanır ve analiz edilir

### 3. Raporlama (Reporting)
- **Özet Raporlar**: Günlük/haftalık/aylık özetler
- **PDF Rapor**: İndirilebilir detaylı raporlar
- **Grafik Görselleştirme**: Zaman serisi grafikleri, heatmap'ler
- **E-posta Raporları**: Zamanlanmış rapor gönderimi

### 4. Uyarı ve Bildirimler (Alerts)
- **Eşik Değer Aşımları**: Belirlenen limitlerin aşılması durumunda uyarı
- **Anlık Bildirimler**: Kritik hava kalitesi durumları
- **Abonelik Sistemi**: Kullanıcıların belirli konumlar için abone olması

---

## 🔧 Sistem Mimarisi

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Server UI                           │
│  ┌──────────┐  ┌──────────────┐  ┌────────────────────┐    │
│  │ Chat.razor│  │EmbedChat.razor│  │AdminDashboard.razor│    │
│  └─────┬────┘  └──────┬───────┘  └─────────┬──────────┘    │
│        │              │                     │                │
│        └──────────────┴─────────────────────┘                │
│                          │                                    │
├──────────────────────────┼────────────────────────────────────┤
│                   Services Layer                              │
│  ┌────────────────────────────────────────────────────┐     │
│  │      ChatOrchestrationService                        │     │
│  │  • Intent Detection (LLM-based + Keyword)           │     │
│  │  • Conversation Context Management                  │     │
│  │  • Multi-step Flow Handling                         │     │
│  │  • Parameter Extraction                            │     │
│  │  • Tenant Context Management                        │     │
│  └────────────────────────────────────────────────────┘     │
│        │              │              │                       │
│  ┌─────┴────┐  ┌─────┴────┐  ┌─────┴──────┐                │
│  │LlmService│  │AirQuality│  │MCP Client  │                │
│  │          │  │Service   │  │Service     │                │
│  └──────────┘  └──────────┘  └────────────┘                │
│        │              │              │                       │
│  ┌─────┴────┐  ┌─────┴────┐  ┌─────┴──────┐                │
│  │Qdrant    │  │PostgreSQL │  │MongoDB    │                │
│  │Service   │  │Service    │  │Service     │                │
│  └──────────┘  └──────────┘  └────────────┘                │
├───────────────────────────────────────────────────────────────┤
│                    Data Layer                                 │
│  ┌──────────────────────────────────────────────────┐       │
│  │      ApplicationDbContext (PostgreSQL)            │       │
│  │  • ChatSessions • ChatMessages • AdminSettings    │       │
│  │  • DomainApiKeys • Users • AuditLogs             │       │
│  │  • AirQualityQueries • SavedReports               │       │
│  │  • TenantMappings (tenant_slug -> domain)         │       │
│  └──────────────────────────────────────────────────┘       │
│  ┌──────────────────────────────────────────────────┐       │
│  │      PostgreSQL - air_quality_index               │       │
│  │  • device_id (tenant'a ait cihazlar)              │       │
│  │  • parameter (PM10-24h, PM2.5-24h, NO2-1h, vb.) │       │
│  │  • concentration, concentration_unit             │       │
│  │  • calculated_datetime                           │       │
│  └──────────────────────────────────────────────────┘       │
│  ┌──────────────────────────────────────────────────┐       │
│  │      MongoDB - airqoonBaseMapDB                   │       │
│  │  • Tenants (SlugName, Name, IsPublic)            │       │
│  │  • Devices (DeviceId, TenantSlugName, Label)     │       │
│  └──────────────────────────────────────────────────┘       │
│  ┌──────────────────────────────────────────────────┐       │
│  │      Qdrant Vector Database                       │       │
│  │  • tenant_{slug} collections (her tenant ayrı)    │       │
│  │  • AnalysisEmbeddings (RAG için)                  │       │
│  │  • Embedding: paraphrase-multilingual-MiniLM    │       │
│  │    (384 dimensions, Türkçe destekli)             │       │
│  └──────────────────────────────────────────────────┘       │
│  ┌──────────────────────────────────────────────────┐       │
│  │      MCP Server (Python)                          │       │
│  │  • mcp_server.py (MCP protocol)                  │       │
│  │  • vector_db_api.py (Qdrant wrapper)             │       │
│  │  • embedding_utils.py (sentence-transformers)    │       │
│  └──────────────────────────────────────────────────┘       │
└───────────────────────────────────────────────────────────────┘
```

---

## 📝 Sistem Prompt'u (LLM İçin)

```
Sen hava kalitesi ölçüm verileri ve analizleri için yardımcı bir asistansın. 
Kullanıcılara hava kalitesi bilgileri, istatistikler, trend analizleri ve raporlar sağla.

⚠️ Asla <think> etiketi veya herhangi bir içsel düşünce gösterme. 
Sadece son cevabı temiz ve Türkçe ver.

📌 Sadece hava kalitesi ölçüm verileri, analizler ve raporlar hakkında soruları cevapla.

🚫 Başka bir konuda soru gelirse, sadece şu cevabı ver: 
"Üzgünüm, sadece hava kalitesi ölçüm verileri ve analizleri hakkında sorulara 
cevap verebilirim. Size hava kalitesi bilgileri konusunda yardımcı olabilirim."

📊 Desteklenen Kirleticiler:
- PM2.5 (İnce partikül madde)
- PM10 (Kaba partikül madde)
- NO2 (Azot dioksit)
- SO2 (Kükürt dioksit)
- CO (Karbon monoksit)
- O3 (Ozon)
- NH3 (Amonyak)
- CO2 (Karbon dioksit)

📍 Desteklenen Sorgu Tipleri:
- Anlık değerler
- Zaman aralığı analizleri
- Karşılaştırmalı analizler
- Trend analizleri
- İstatistiksel özetler
- Grafik görselleştirmeleri
```

---

## 🔄 Intent Detection Akışı

```
Kullanıcı Mesajı
       │
       ▼
┌──────────────────┐
│ Security Check   │ → Spam/Invalid → Reject
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Simple Response? │ → Greeting/Thanks/Goodbye → Predefined Response
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Context Check    │ → Awaiting Parameter? → Continue Flow
└────────┬─────────┘
         │
         ▼
┌──────────────────────┐
│ LLM Intent Detection │
└────────┬─────────────┘
         │
    ┌────┴────┬─────────────┬──────────────┐
    ▼         ▼             ▼              ▼
AirQuality  Statistical  Comparison   ReportRequest
Query       Analysis     Analysis
    │         │             │              │
    ▼         ▼             ▼              ▼
Collect    Analyze      Compare       Generate
Parameters Time Series  Periods       Report
```

---

## 🎨 Intent Tipleri

### 1. AirQualityQuery
**Parametreler:**
- `tenantSlug`: Tenant slug (örn: "akcansa", "tupras", "bursa-metropolitan-municipality")
- `pollutant`: PM2.5, PM10, NO2, vb. (normalize edilir: PM10-24h, PM2.5-24h, NO2-1h)
- `startDate`: Başlangıç tarihi (YYYY-MM-DD)
- `endDate`: Bitiş tarihi (YYYY-MM-DD)
- `date`: Tek tarih sorgusu
- `aggregation`: average, max, min, current

**Örnek Sorgular:**
- "Akçansa'da bugünkü PM2.5 değeri nedir?"
- "Tüpraş'ta son 7 günün PM10 ortalaması"
- "Bursa Büyükşehir Belediyesi için 2024 Ocak ayı NO2 verileri"

### 2. StatisticalAnalysis (Time Range Analysis)
**Parametreler:**
- `tenantSlug`: Tenant slug
- `startDate`: Başlangıç tarihi (YYYY-MM-DD)
- `endDate`: Bitiş tarihi (YYYY-MM-DD)
- `comparisonStartDate`: Karşılaştırma başlangıç tarihi (opsiyonel)
- `comparisonEndDate`: Karşılaştırma bitiş tarihi (opsiyonel)
- `pollutants`: Kirletici listesi (varsayılan: ["PM2.5", "PM10", "NO2"])

**Örnek Sorgular:**
- "Akçansa'nın Şubat ve Nisan ayları arasındaki farklılıkları analiz et"
- "Tüpraş'ta son 3 ayın PM2.5 trend analizi"
- "Bursa için PM10 dağılım istatistikleri"

### 3. ComparisonAnalysis (Monthly Comparison)
**Parametreler:**
- `tenantSlug`: Tenant slug
- `month1`: İlk ay (YYYY-MM formatında, örn: "2025-02")
- `month2`: İkinci ay (YYYY-MM formatında, örn: "2025-04")
- `year`: Yıl (opsiyonel, belirtilmezse her ayın kendi yılı kullanılır)

**Özellikler:**
- İki ay arasındaki dramatik değişiklikleri tespit eder (%20+ değişim vurgulanır)
- PM2.5, PM10, NO2, O3 parametrelerini analiz eder
- Sonuçlar otomatik olarak vector DB'ye kaydedilir (RAG için)

**Örnek Sorgular:**
- "Akçansa'nın Şubat 2025 ve Nisan 2025 ayları arasındaki farkları analiz et"
- "Tüpraş'ta Ocak ve Şubat ayları karşılaştırması"
- "Bursa için bu ay geçen ay ile karşılaştır"

### 4. ReportRequest
**Parametreler:**
- `tenantSlug`: Tenant slug
- `startDate`: Başlangıç
- `endDate`: Bitiş
- `reportType`: summary, detailed, pdf
- `format`: json, pdf, excel

**Örnek Sorgular:**
- "Akçansa için aylık rapor oluştur"
- "Tüpraş'ta son haftanın özet raporunu PDF olarak indir"
- "Bursa için detaylı analiz raporu hazırla"

---

## 🗃️ Veritabanı Şeması

### PostgreSQL (Chat & Admin Veritabanı)

```sql
-- Chat Sessions
CREATE TABLE ChatSessions (
    SessionId VARCHAR(255) PRIMARY KEY,
    Domain VARCHAR(255),
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(500),
    CreatedAt TIMESTAMP NOT NULL,
    LastActivityAt TIMESTAMP,
    IsActive BOOLEAN DEFAULT true,
    INDEX idx_created_at (CreatedAt),
    INDEX idx_domain (Domain)
);

-- Chat Messages
CREATE TABLE ChatMessages (
    Id SERIAL PRIMARY KEY,
    SessionId VARCHAR(255) NOT NULL,
    IsUser BOOLEAN NOT NULL,
    Content TEXT NOT NULL,
    Timestamp TIMESTAMP NOT NULL,
    ErrorMessage TEXT,
    IntentType VARCHAR(50),
    ParametersJson JSONB,
    ResponseDataJson JSONB,
    FOREIGN KEY (SessionId) REFERENCES ChatSessions(SessionId) ON DELETE CASCADE,
    INDEX idx_session_id (SessionId),
    INDEX idx_timestamp (Timestamp)
);

-- Conversation Contexts
CREATE TABLE ConversationContexts (
    SessionId VARCHAR(255) PRIMARY KEY,
    CurrentIntent VARCHAR(50),
    CollectedParametersJson JSONB,
    Location VARCHAR(255),
    Pollutant VARCHAR(50),
    StartDate DATE,
    EndDate DATE,
    LastActivity TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    FOREIGN KEY (SessionId) REFERENCES ChatSessions(SessionId) ON DELETE CASCADE,
    INDEX idx_last_activity (LastActivity)
);

-- Admin Settings
CREATE TABLE AdminSettings (
    Id INTEGER PRIMARY KEY DEFAULT 1,
    LlmProvider VARCHAR(50) NOT NULL,
    ModelName VARCHAR(100),
    ApiKey TEXT,
    OllamaBaseUrl VARCHAR(255),
    SystemPrompt TEXT,
    Temperature DECIMAL(3,2) DEFAULT 0.7,
    MaxTokens INTEGER DEFAULT 2000,
    ApiBaseUrl VARCHAR(255),
    UpdatedAt TIMESTAMP NOT NULL,
    CONSTRAINT single_row CHECK (Id = 1)
);

-- Domain API Keys
CREATE TABLE DomainApiKeys (
    Id SERIAL PRIMARY KEY,
    Domain VARCHAR(255) UNIQUE NOT NULL,
    ApiKey VARCHAR(255) UNIQUE NOT NULL,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,
    INDEX idx_domain (Domain),
    INDEX idx_api_key (ApiKey)
);

-- Domain Appearance
CREATE TABLE DomainAppearances (
    Id SERIAL PRIMARY KEY,
    Domain VARCHAR(255) UNIQUE NOT NULL,
    ChatbotName VARCHAR(255),
    ChatbotLogoUrl TEXT,
    PrimaryColor VARCHAR(7),
    SecondaryColor VARCHAR(7),
    WelcomeMessage TEXT,
    ChatbotOnline BOOLEAN DEFAULT true,
    OpenChatOnLoad BOOLEAN DEFAULT true,
    QuickRepliesJson JSONB,
    GreetingResponse TEXT,
    ThanksResponse TEXT,
    UpdatedAt TIMESTAMP NOT NULL,
    INDEX idx_domain (Domain)
);

-- Users
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Email VARCHAR(255),
    Role VARCHAR(50) DEFAULT 'Admin',
    CreatedAt TIMESTAMP NOT NULL,
    LastLoginAt TIMESTAMP,
    IsActive BOOLEAN DEFAULT true,
    INDEX idx_username (Username)
);

-- Audit Logs
CREATE TABLE AuditLogs (
    Id SERIAL PRIMARY KEY,
    Action VARCHAR(100) NOT NULL,
    Details TEXT,
    UserId INTEGER,
    IpAddress VARCHAR(45),
    Timestamp TIMESTAMP NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    INDEX idx_timestamp (Timestamp),
    INDEX idx_user_id (UserId),
    INDEX idx_action (Action)
);

-- Saved Air Quality Queries
CREATE TABLE SavedAirQualityQueries (
    Id SERIAL PRIMARY KEY,
    SessionId VARCHAR(255),
    QueryType VARCHAR(50),
    Location VARCHAR(255),
    Pollutant VARCHAR(50),
    StartDate DATE,
    EndDate DATE,
    ParametersJson JSONB,
    ResultSummary TEXT,
    CreatedAt TIMESTAMP NOT NULL,
    FOREIGN KEY (SessionId) REFERENCES ChatSessions(SessionId) ON DELETE SET NULL,
    INDEX idx_session_id (SessionId),
    INDEX idx_created_at (CreatedAt)
);

-- Saved Reports
CREATE TABLE SavedReports (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER,
    ReportName VARCHAR(255),
    ReportType VARCHAR(50),
    TenantSlug VARCHAR(255),
    StartDate DATE,
    EndDate DATE,
    ReportDataJson JSONB,
    FilePath TEXT,
    CreatedAt TIMESTAMP NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_id (UserId),
    INDEX idx_created_at (CreatedAt),
    INDEX idx_tenant_slug (TenantSlug)
);

-- Domain to Tenant Mapping (Domain -> Tenant Slug eşleştirmesi)
CREATE TABLE DomainTenantMappings (
    Id SERIAL PRIMARY KEY,
    Domain VARCHAR(255) UNIQUE NOT NULL,
    TenantSlug VARCHAR(255) NOT NULL,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL,
    INDEX idx_domain (Domain),
    INDEX idx_tenant_slug (TenantSlug)
);
```

### PostgreSQL (Hava Kalitesi Ölçüm Verileri)

```sql
-- air_quality_index tablosu (device_id bazlı ölçümler)
CREATE TABLE air_quality_index (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(255) NOT NULL,           -- Tenant'a ait cihaz ID'si
    parameter VARCHAR(50) NOT NULL,             -- PM10-24h, PM2.5-24h, NO2-1h, O3-1h, SO2-1h, CO-8h
    concentration DECIMAL(10, 2),               -- Konsantrasyon değeri
    concentration_unit VARCHAR(20),             -- µg/m³, mg/m³, ppm
    calculated_datetime TIMESTAMP NOT NULL,     -- Ölçüm zamanı
    created_at TIMESTAMP DEFAULT NOW(),
    INDEX idx_device_id (device_id),
    INDEX idx_parameter (parameter),
    INDEX idx_calculated_datetime (calculated_datetime),
    INDEX idx_device_datetime (device_id, calculated_datetime)
);

-- Parametre Normalizasyonu:
-- PM10 -> PM10-24h
-- PM2.5 veya PM25 -> PM2.5-24h
-- NO2 -> NO2-1h
-- O3 -> O3-1h
-- SO2 -> SO2-1h
-- CO -> CO-8h
```

### MongoDB (Tenant & Device Metadata)

```javascript
// Database: airqoonBaseMapDB

// Tenants Collection
{
  _id: ObjectId,
  SlugName: String,           // "akcansa", "tupras", "bursa-metropolitan-municipality"
  Name: String,               // "Akçansa", "Tüpraş", "Bursa Büyükşehir Belediyesi"
  IsPublic: Boolean,          // Public/Private durumu
  // ... diğer tenant bilgileri
}

// Devices Collection
{
  _id: ObjectId,
  DeviceId: String,           // PostgreSQL'deki device_id ile eşleşir
  TenantSlugName: String,     // Tenant slug (Tenants.SlugName ile eşleşir)
  Name: String,               // Cihaz adı
  Label: String,              // Cihaz etiketi
  LatestTelemetry: Object,   // Son telemetri verileri
  // ... diğer cihaz bilgileri
}

// Indexes
db.Tenants.createIndex({ SlugName: 1 }, { unique: true });
db.Devices.createIndex({ TenantSlugName: 1 });
db.Devices.createIndex({ DeviceId: 1 });
```

### Qdrant Vector Database (RAG için)

**ÖNEMLİ: Tenant İzolasyonu**
- Her tenant'ın **ayrı collection'ı** var: `tenant_{slug}`
- Örnek: `tenant_akcansa`, `tenant_tupras`, `tenant_bursa-metropolitan-municipality`
- 3 katmanlı güvenlik: Collection seviyesi, API seviyesi, Payload seviyesi

```python
# Qdrant Collection Yapısı (her tenant için ayrı)
Collection Name: tenant_{tenant_slug}
Vector Size: 384 dimensions
Distance Metric: COSINE
Model: paraphrase-multilingual-MiniLM-L12-v2 (Türkçe destekli)

# Point Structure
{
  "id": String,                    # Vector ID (hash-based, tenant prefix ile)
  "vector": [float] * 384,         # Embedding vector
  "payload": {
    "_tenant": String,             # Tenant slug (double-check için)
    "text": String,                 # Analiz metni
    "type": String,                 # "analysis"
    "analysis_type": String,       # "monthly_comparison", "time_range_analysis"
    "created_at": String,          # ISO timestamp
    "start_date": String,          # YYYY-MM-DD (varsa)
    "end_date": String,            # YYYY-MM-DD (varsa)
    "tenant_name": String,         # Tenant adı
    "device_count": Integer,       # Cihaz sayısı
    // ... diğer metadata
  }
}

# Collection Setup (vector_db_setup.py)
from qdrant_client.models import VectorParams, Distance

client.create_collection(
    collection_name=f"tenant_{tenant_slug}",
    vectors_config=VectorParams(
        size=384,                    # paraphrase-multilingual-MiniLM-L12-v2 dimension
        distance=Distance.COSINE
    )
)
```

---

## 🔌 MCP (Model Context Protocol) Entegrasyonu

### MCP Server (Python)

MCP Server Python'da çalışır ve Cursor IDE ile entegre edilir. .NET uygulaması MCP server'a HTTP veya stdio üzerinden bağlanır.

**MCP Server Dosyaları:**
- `mcp_server.py`: Ana MCP server (MCP protocol)
- `vector_db_api.py`: Qdrant API wrapper (tenant isolated)
- `embedding_utils.py`: Embedding generation (sentence-transformers)
- `vector_db_setup.py`: Qdrant collection setup

### MCP Tools (mcp_server.py)

```python
# MCP Tools Listesi
1. tenant_time_range_analysis
   - Parametreler: tenant_slug, start_date, end_date, comparison_start_date (opsiyonel), 
                    comparison_end_date (opsiyonel), pollutants (opsiyonel)
   - Açıklama: Tenant'ın belirli bir zaman aralığındaki hava kalitesi verilerini analiz eder
   - Örnek: "Akçansa'nın Şubat ve Nisan ayları arasındaki farklılıkları analiz et"

2. tenant_monthly_comparison
   - Parametreler: tenant_slug, month1 (YYYY-MM), month2 (YYYY-MM), year (opsiyonel)
   - Açıklama: İki ay arasındaki dramatik farklılıkları analiz eder (%20+ değişim vurgulanır)
   - Örnek: "Akçansa'nın Şubat 2025 ve Nisan 2025 ayları arasındaki farkları analiz et"

3. tenant_device_list
   - Parametreler: tenant_slug
   - Açıklama: Tenant'a ait tüm cihazları listeler

4. tenant_statistics
   - Parametreler: tenant_slug
   - Açıklama: Tenant'ın genel istatistiklerini döndürür (cihaz sayısı, vector DB points, vb.)

5. save_analysis_to_vector_db
   - Parametreler: tenant_slug, analysis_text, analysis_type (opsiyonel), metadata (opsiyonel)
   - Açıklama: Analiz sonuçlarını vector database'e kaydet (RAG için)

6. search_analysis_from_vector_db
   - Parametreler: tenant_slug, query_text, limit (opsiyonel), score_threshold (opsiyonel), 
                    filter_type (opsiyonel)
   - Açıklama: Vector database'den RAG ile analiz sonuçlarını ara (semantic search)
```

### .NET MCP Client Service

```csharp
// Services/IAirQualityMcpService.cs
public interface IAirQualityMcpService
{
    // MCP Server'a bağlan (stdio veya HTTP)
    Task<TimeRangeAnalysisResult> TenantTimeRangeAnalysisAsync(
        string tenantSlug,
        DateTime startDate,
        DateTime endDate,
        List<string> pollutants = null,
        DateTime? comparisonStartDate = null,
        DateTime? comparisonEndDate = null);
    
    Task<MonthlyComparisonResult> TenantMonthlyComparisonAsync(
        string tenantSlug,
        string month1,  // YYYY-MM
        string month2,  // YYYY-MM
        int? year = null);
    
    Task<List<DeviceInfo>> GetTenantDevicesAsync(string tenantSlug);
    Task<TenantStatistics> GetTenantStatisticsAsync(string tenantSlug);
    
    // Vector DB işlemleri (Qdrant üzerinden)
    Task<string> SaveAnalysisToVectorDbAsync(
        string tenantSlug,
        string analysisText,
        string analysisType = "analysis",
        Dictionary<string, object> metadata = null);
    
    Task<List<AnalysisSearchResult>> SearchAnalysisFromVectorDbAsync(
        string tenantSlug,
        string queryText,
        int limit = 5,
        double scoreThreshold = 0.5,
        string filterType = null);
}

// Services/AirQualityMcpService.cs
public class AirQualityMcpService : IAirQualityMcpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AirQualityMcpService> _logger;
    
    // MCP Server'a stdio veya HTTP üzerinden bağlan
    // Örnek: Process.Start ile Python MCP server'ı başlat ve stdio üzerinden iletişim kur
}
```

### MCP Config (Cursor IDE)

```json
// .cursor/mcp.json veya mcp_config.json
{
  "mcpServers": {
    "airqoon-analyzer": {
      "command": "python3",
      "args": ["/path/to/Airqoon/mcp_server.py"],
      "env": {
        "PGUSER": "postgres_user",
        "PYTHONPATH": "/path/to/Airqoon"
      }
    }
  }
}
```

---

## 🎨 Embed Widget Özellikleri

```html
<!-- Harici sitede kullanım -->
<iframe
  src="https://airquality-chatbot.domain.com/chatbot/embed?apiKey=API_KEY&domain=site.com"
  style="width: 400px; height: 600px; border: none;">
</iframe>
```

**Domain başına özelleştirme:**
- Chatbot adı ve logo
- Renk teması (primary/secondary)
- Karşılama mesajı
- Hızlı yanıt butonları ("Hava kalitesi sorgula", "Grafik göster", vb.)
- Özel yanıtlar (selamlama, teşekkür, vb.)

---

## 🛡️ Güvenlik Özellikleri

| Özellik | Açıklama |
|---------|----------|
| Input Validation | Max 400 karakter, XSS koruması |
| Spam Detection | Spam pattern tespiti |
| Rate Limiting | IP başına istek limiti (dakikada 30 istek) |
| Session Security | IP/UserAgent doğrulama |
| Audit Logging | Tüm işlemler loglanır |
| API Key Auth | Domain bazlı API anahtarları |
| Data Encryption | Hassas veriler şifrelenir |

---

## 📊 Admin Dashboard

**Özellikler:**
- **LLM Ayarları**: Provider ve model seçimi (Ollama, OpenAI, Anthropic)
- **Chatbot Ayarları**: İsim, renk, mesajlar, hızlı yanıtlar
- **Domain Yönetimi**: 
  - API key oluşturma
  - Görünüm özelleştirme (DomainAppearance)
  - Domain -> Tenant mapping (DomainTenantMappings)
- **Tenant Yönetimi**:
  - Tenant listesi (MongoDB'den)
  - Tenant detayları (cihaz sayısı, istatistikler)
  - Tenant slug -> Domain mapping
- **Kullanıcı Yönetimi**: Kullanıcı ekleme/düzenleme, rol yönetimi
- **Session İzleme**: Aktif session'lar, son aktiviteler, tenant bazlı filtreleme
- **Analytics Dashboard**: 
  - Toplam sorgu sayısı
  - En çok sorgulanan tenant'lar
  - En çok sorgulanan kirleticiler
  - Zaman bazlı istatistikler
  - Kullanıcı davranış analizi
  - Tenant bazlı analytics
- **Audit Log Görüntüleme**: Tüm işlemlerin logları
- **Rapor Yönetimi**: Oluşturulan raporları görüntüleme/indirme
- **Vector DB Yönetimi**: RAG verilerini yönetme, tenant bazlı collection'lar

---

## 🚀 Çalıştırma

```bash
# Development
cd AirQualityChatBot
dotnet run

# Production
dotnet publish -c Release
```

**URL'ler:**
- Chat: `http://localhost:5000/chatbot/chat`
- Admin: `http://localhost:5000/chatbot/admin`
- Embed: `http://localhost:5000/chatbot/embed?apiKey=...&domain=...`
- MCP API: `http://localhost:5000/api/mcp/airquality/...`

---

## 📦 Teknoloji Stack

- **.NET 8.0** - Backend framework
- **Blazor Server** - UI framework
- **Entity Framework Core** - ORM (PostgreSQL)
- **MongoDB.Driver** - MongoDB client
- **MongoDB Atlas Vector Search** - Vector database
- **PostgreSQL** - Ana veritabanı
- **MongoDB** - Time series veritabanı
- **Ollama / OpenAI / Anthropic** - LLM providers
- **TailwindCSS + TailAdmin** - UI tasarım
- **Chart.js / D3.js** - Grafik görselleştirme

---

## 📁 Proje Yapısı

```
AirQualityChatBot/
├── Components/
│   ├── Pages/
│   │   ├── Chat.razor              # Ana chat sayfası
│   │   ├── EmbedChat.razor         # Embed widget sayfası
│   │   └── AdminDashboard.razor    # Admin paneli
│   ├── ChatWidget.razor            # Chat bileşeni
│   └── Layout/                     # Layout bileşenleri
├── Controllers/
│   ├── ChatController.cs           # REST API endpoints
│   └── AirQualityMcpController.cs  # MCP endpoints
├── Data/
│   ├── ApplicationDbContext.cs     # EF DbContext (PostgreSQL)
│   └── Entities/                   # Veritabanı entity'leri
├── Models/
│   ├── DTOs/                       # Data transfer objects
│   │   ├── AirQualityQueryResult.cs
│   │   ├── StatisticalAnalysisResult.cs
│   │   ├── ComparisonResult.cs
│   │   └── IntentDetectionResult.cs
│   └── AirQualityModels.cs         # Domain models
├── Services/
│   ├── ChatOrchestrationService.cs # Ana orkestrasyon servisi
│   ├── LlmService.cs               # LLM iletişimi
│   ├── AirQualityService.cs       # Hava kalitesi veri servisi
│   ├── AirQualityMcpService.cs    # MCP servis implementasyonu
│   ├── VectorDbService.cs          # Vector DB işlemleri (Qdrant)
│   ├── MongoDbService.cs           # MongoDB işlemleri
│   ├── PostgresAirQualityService.cs # PostgreSQL hava kalitesi verileri
│   ├── TenantMappingService.cs     # Domain -> Tenant mapping
│   ├── AnalyticsService.cs         # Analytics servisi
│   ├── ReportService.cs            # Rapor oluşturma servisi
│   ├── AdminSettingsService.cs    # Admin ayarları
│   ├── DomainApiKeyService.cs      # API key yönetimi
│   ├── DomainAppearanceService.cs  # Domain görünüm özelleştirme
│   ├── SecurityService.cs          # Güvenlik servisleri
│   └── SessionManagementService.cs # Session yönetimi
├── wwwroot/
│   ├── app.css                     # Ana stiller
│   ├── chatbot-widget.css          # Widget stilleri
│   └── charts.js                   # Grafik kütüphanesi
├── appsettings.json                # Uygulama ayarları
└── appsettings.llm.json            # LLM ayarları (runtime)
```

---

## 🔑 Konfigürasyon

### appsettings.json

```json
{
  "Database": {
    "UsePostgreSQL": true,
    "UseMemoryForContext": true
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=airqualitychatbot;Username=postgres;Password=YourPassword",
    "PostgreSQLAirQuality": "Host=localhost;Port=5432;Database=airqoon;Username=postgres;Password=YourPassword",
    "MongoDB": "mongodb://localhost:27017"
  },
  "LlmSettings": {
    "Provider": "Ollama",
    "ModelName": "qwen3:32b",
    "OllamaBaseUrl": "http://localhost:11434",
    "Temperature": 0.7,
    "MaxTokens": 2000
  },
  "VectorDb": {
    "Provider": "Qdrant",
    "QdrantHost": "localhost",
    "QdrantPort": 6333,
    "QdrantApiKey": null,
    "EmbeddingModel": "paraphrase-multilingual-MiniLM-L12-v2",
    "EmbeddingDimensions": 384,
    "CollectionPrefix": "tenant_"
  },
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017/",
    "Database": "airqoonBaseMapDB",
    "TenantsCollection": "Tenants",
    "DevicesCollection": "Devices"
  },
  "PostgreSQL": {
    "AirQualityDatabase": "airqoon",
    "AirQualityTable": "air_quality_index"
  },
  "AirQuality": {
    "DefaultPollutants": ["PM2.5", "PM10", "NO2", "SO2", "CO", "O3"],
    "CacheDurationMinutes": 5,
    "MaxQueryDays": 365
  },
  "RateLimiting": {
    "RequestsPerMinute": 30,
    "RequestsPerHour": 500
  }
}
```

---

## 📞 API Endpoints

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/chat/message` | Mesaj gönder |
| GET | `/api/chat/session/{id}` | Session bilgisi |
| POST | `/api/chat/clear/{id}` | Session temizle |
| POST | `/api/mcp/airquality/tenant/{slug}/time-range-analysis` | Zaman aralığı analizi (MCP tool çağrısı) |
| POST | `/api/mcp/airquality/tenant/{slug}/monthly-comparison` | Aylık karşılaştırma (MCP tool çağrısı) |
| GET | `/api/mcp/airquality/tenant/{slug}/devices` | Cihaz listesi (MCP tool çağrısı) |
| GET | `/api/mcp/airquality/tenant/{slug}/statistics` | Tenant istatistikleri (MCP tool çağrısı) |
| POST | `/api/mcp/airquality/vector/save` | Analiz kaydet (Qdrant, RAG için) |
| POST | `/api/mcp/airquality/vector/search` | Analiz ara (Qdrant, semantic search) |

---

## 🔄 ChatOrchestrationService Akışı

### 1. Mesaj İşleme

```csharp
public async Task<ChatResponse> ProcessMessageAsync(string userMessage, string? sessionId = null)
{
    // 1. Güvenlik kontrolü
    if (!_securityService.IsValidInput(userMessage)) { ... }
    if (_securityService.ContainsSpam(userMessage)) { ... }
    
    // 2. Session yönetimi
    sessionId = await EnsureSessionAsync(sessionId);
    
    // 3. Context yükleme
    var context = await GetOrCreateContextAsync(sessionId);
    
    // 4. Basit yanıt kontrolü (selamlama, teşekkür, vb.)
    var simpleResponse = await GetSimpleResponseAsync(userMessage);
    if (simpleResponse != null) return simpleResponse;
    
    // 5. Intent detection (LLM + Keyword hybrid)
    var intentResult = await _llmService.DetectIntentAsync(userMessage, settings.SystemPrompt, context);
    
    // 6. Parametre birleştirme
    MergeParameters(context, intentResult);
    
    // 7. Intent'e göre işleme
    switch (intentResult.Intent)
    {
        case IntentType.AirQualityQuery:
            return await HandleAirQualityQuery(intentResult, context, userMessage);
        case IntentType.StatisticalAnalysis:
            return await HandleStatisticalAnalysis(intentResult, context, userMessage);
        case IntentType.ComparisonAnalysis:
            return await HandleComparisonAnalysis(intentResult, context, userMessage);
        case IntentType.ReportRequest:
            return await HandleReportRequest(intentResult, context, userMessage);
        case IntentType.GeneralQuestion:
            return await HandleGeneralQuestion(userMessage, settings);
    }
}
```

### 2. Hava Kalitesi Sorgusu İşleme

```csharp
private async Task<ChatResponse> HandleAirQualityQuery(
    IntentDetectionResult intent, 
    ConversationContext context, 
    string userMessage)
{
    // 1. Tenant slug belirleme (öncelik sırası):
    //    a) Intent'ten gelen tenantSlug
    //    b) Context'teki tenantSlug
    //    c) Domain'den mapping (DomainTenantMappings tablosu)
    //    d) Kullanıcıdan sor
    var tenantSlug = ExtractTenantSlug(intent, context, userMessage);
    
    // Domain'den tenant mapping (eğer tenantSlug yoksa)
    if (string.IsNullOrEmpty(tenantSlug) && !string.IsNullOrEmpty(context.Domain))
    {
        tenantSlug = await _tenantMappingService.GetTenantSlugByDomainAsync(context.Domain);
        if (!string.IsNullOrEmpty(tenantSlug))
        {
            context.TenantSlug = tenantSlug;
            await SaveContextAsync(context);
        }
    }
    
    var pollutant = ExtractPollutant(intent, context, userMessage);
    var dateRange = ExtractDateRange(intent, context, userMessage);
    
    // 2. Eksik parametre kontrolü
    if (string.IsNullOrEmpty(tenantSlug))
    {
        context.CurrentIntent = IntentType.AirQualityQuery;
        await SaveContextAsync(context);
        return new ChatResponse { 
            Message = "Hangi kurum/şirket için hava kalitesi bilgisi istiyorsunuz? (Örnek: Akçansa, Tüpraş, Bursa Büyükşehir Belediyesi)" 
        };
    }
    
    // 3. Tenant doğrulama (MongoDB'den)
    var tenant = await _mongoDbService.GetTenantAsync(tenantSlug);
    if (tenant == null)
    {
        context.TenantInvalidAttempts++;
        if (context.TenantInvalidAttempts >= 3)
        {
            context.TenantSlug = null;
            context.CurrentIntent = null;
            await SaveContextAsync(context);
            return new ChatResponse { 
                Message = "Tenant bulunamadı. Lütfen geçerli bir kurum/şirket adı girin." 
            };
        }
        return new ChatResponse { 
            Message = $"'{tenantSlug}' için veri bulunamadı. Lütfen geçerli bir kurum/şirket adı girin." 
        };
    }
    
    // 4. MCP Server üzerinden analiz yap
    var analysisResult = await _airQualityMcpService.TenantTimeRangeAnalysisAsync(
        tenantSlug: tenantSlug,
        startDate: dateRange.StartDate,
        endDate: dateRange.EndDate,
        pollutants: pollutant != null ? new List<string> { pollutant } : null
    );
    
    // 5. Grafik verisi hazırlama (PostgreSQL'den raw data çek)
    var measurements = await _postgresAirQualityService.GetAirQualityDataAsync(
        tenantSlug, 
        dateRange.StartDate, 
        dateRange.EndDate, 
        pollutant);
    var chartData = PrepareChartData(measurements, pollutant);
    
    // 6. Context'i güncelle
    context.TenantSlug = tenantSlug;
    context.Pollutant = pollutant;
    context.StartDate = dateRange.StartDate;
    context.EndDate = dateRange.EndDate;
    await SaveContextAsync(context);
    
    // 7. Response oluşturma
    return new ChatResponse
    {
        Message = FormatAirQualityResponse(analysisResult, tenantSlug, pollutant),
        AirQualityData = analysisResult,
        ChartData = chartData,
        ShowChart = true
    };
}

// Tenant Slug Extraction Helper
private string? ExtractTenantSlug(IntentDetectionResult intent, ConversationContext context, string userMessage)
{
    // 1. Intent'ten gelen tenantSlug
    if (intent.Parameters.TryGetValue("tenantSlug", out var intentTenant) && !string.IsNullOrWhiteSpace(intentTenant))
    {
        return NormalizeTenantSlug(intentTenant);
    }
    
    // 2. Context'teki tenantSlug
    if (!string.IsNullOrWhiteSpace(context.TenantSlug))
    {
        return context.TenantSlug;
    }
    
    // 3. Kullanıcı mesajından tenant adını çıkar (LLM veya keyword matching)
    var tenantName = ExtractTenantNameFromMessage(userMessage);
    if (!string.IsNullOrWhiteSpace(tenantName))
    {
        // Tenant adını slug'a çevir (MongoDB'den lookup)
        return _tenantMappingService.ConvertTenantNameToSlugAsync(tenantName).Result;
    }
    
    return null;
}

// Tenant Name -> Slug Conversion
// Örnek: "Akçansa" -> "akcansa", "Bursa Büyükşehir Belediyesi" -> "bursa-metropolitan-municipality"
```

### 3. İstatistiksel Analiz İşleme

```csharp
private async Task<ChatResponse> HandleStatisticalAnalysis(
    IntentDetectionResult intent,
    ConversationContext context,
    string userMessage)
{
    var tenantSlug = ExtractTenantSlug(intent, context, userMessage);
    var startDate = ExtractStartDate(intent, context);
    var endDate = ExtractEndDate(intent, context);
    var comparisonStart = ExtractComparisonStartDate(intent, context);
    var comparisonEnd = ExtractComparisonEndDate(intent, context);
    var pollutants = ExtractPollutants(intent, context) ?? new List<string> { "PM2.5", "PM10", "NO2" };
    
    // 1. MCP servisini kullanarak analiz yap
    var analysisResult = await _airQualityMcpService.TenantTimeRangeAnalysisAsync(
        tenantSlug: tenantSlug,
        startDate: startDate,
        endDate: endDate,
        pollutants: pollutants,
        comparisonStartDate: comparisonStart,
        comparisonEndDate: comparisonEnd
    );
    
    // NOT: MCP Server otomatik olarak analizi Qdrant'a kaydeder
    // Bu yüzden manuel kaydetme gerekmez, ancak isterseniz tekrar kaydedebilirsiniz
    
    // 2. Response formatla
    return new ChatResponse
    {
        Message = FormatStatisticalAnalysisResponse(analysisResult),
        StatisticalData = analysisResult,
        ShowChart = true,
        ChartData = PrepareStatisticalChart(analysisResult)
    };
}
```

### 4. Aylık Karşılaştırma İşleme

```csharp
private async Task<ChatResponse> HandleMonthlyComparison(
    IntentDetectionResult intent,
    ConversationContext context,
    string userMessage)
{
    var tenantSlug = ExtractTenantSlug(intent, context, userMessage);
    var month1 = ExtractMonth1(intent, context); // YYYY-MM
    var month2 = ExtractMonth2(intent, context); // YYYY-MM
    var year = ExtractYear(intent, context); // Opsiyonel
    
    // MCP servisini kullanarak aylık karşılaştırma yap
    var comparisonResult = await _airQualityMcpService.TenantMonthlyComparisonAsync(
        tenantSlug: tenantSlug,
        month1: month1,
        month2: month2,
        year: year
    );
    
    // MCP Server otomatik olarak:
    // 1. İki ayın verilerini analiz eder
    // 2. Dramatik değişiklikleri tespit eder (%20+ değişim)
    // 3. Sonuçları Qdrant'a kaydeder (RAG için)
    
    return new ChatResponse
    {
        Message = FormatMonthlyComparisonResponse(comparisonResult),
        ComparisonData = comparisonResult,
        ShowChart = true,
        ChartData = PrepareComparisonChart(comparisonResult)
    };
}
```

---

## 🎯 Intent Detection Prompt'u

```csharp
private string BuildIntentDetectionPrompt(string userMessage)
{
    return $@"Analiz et aşağıdaki kullanıcı mesajını ve intent'i belirle. 
SADECE JSON formatında cevap ver:

{{
  ""intent"": ""AirQualityQuery|StatisticalAnalysis|ComparisonAnalysis|ReportRequest|GeneralQuestion"",
  ""parameters"": {{
    ""tenantSlug"": ""<tenant slug: akcansa, tupras, bursa-metropolitan-municipality, vb.>"",
    ""pollutant"": ""<PM2.5|PM10|NO2|SO2|CO|O3>"",
    ""startDate"": ""<YYYY-MM-DD>"",
    ""endDate"": ""<YYYY-MM-DD>"",
    ""date"": ""<YYYY-MM-DD>"",
    ""aggregation"": ""<average|max|min|current>"",
    ""month1"": ""<YYYY-MM>"",
    ""month2"": ""<YYYY-MM>"",
    ""year"": <integer>,
    ""comparisonStartDate"": ""<YYYY-MM-DD>"",
    ""comparisonEndDate"": ""<YYYY-MM-DD>"",
    ""reportType"": ""<summary|detailed|pdf>""
  }},
  ""requiresClarification"": <true|false>,
  ""clarificationMessage"": ""<açıklama mesajı>""
}}

Kullanıcı mesajı: {userMessage}

Örnekler:
- ""Akçansa'da bugünkü PM2.5 değeri nedir?"" 
  -> intent: AirQualityQuery, tenantSlug: akcansa, pollutant: PM2.5, date: bugün

- ""Tüpraş'ta son 7 günün PM10 ortalaması""
  -> intent: AirQualityQuery, tenantSlug: tupras, pollutant: PM10, aggregation: average, startDate: 7 gün önce

- ""Akçansa'nın Şubat ve Nisan ayları arasındaki farklılıkları analiz et""
  -> intent: StatisticalAnalysis, tenantSlug: akcansa, month1: 2025-02, month2: 2025-04

- ""Bursa için Ocak ve Şubat ayları karşılaştırması""
  -> intent: ComparisonAnalysis, tenantSlug: bursa-metropolitan-municipality, month1: 2025-01, month2: 2025-02

- ""Akçansa için aylık rapor oluştur""
  -> intent: ReportRequest, tenantSlug: akcansa, reportType: summary

NOT: Tenant slug'ları şunlar olabilir: akcansa, tupras, bursa-metropolitan-municipality, vb.
Kullanıcı tenant adını söylediğinde (örn: ""Akçansa"", ""Tüpraş"") slug'a çevir.";
}
```

---

## 📊 Grafik ve Görselleştirme

### Chart Data Format

```csharp
public class ChartData
{
    public string Type { get; set; }  // "line", "bar", "heatmap"
    public ChartConfig Config { get; set; }
    public List<ChartSeries> Series { get; set; }
    public ChartAxis XAxis { get; set; }
    public ChartAxis YAxis { get; set; }
}

public class ChartSeries
{
    public string Name { get; set; }
    public string Pollutant { get; set; }
    public List<ChartDataPoint> Data { get; set; }
}

public class ChartDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string QualityLevel { get; set; }
}
```

### Chart.js Integration

```javascript
// wwwroot/charts.js
function renderAirQualityChart(chartData) {
    const ctx = document.getElementById('airQualityChart');
    new Chart(ctx, {
        type: chartData.type || 'line',
        data: {
            labels: chartData.series[0].data.map(d => formatDate(d.timestamp)),
            datasets: chartData.series.map(series => ({
                label: series.name,
                data: series.data.map(d => d.value),
                borderColor: getPollutantColor(series.pollutant),
                backgroundColor: getPollutantColor(series.pollutant, 0.1),
                tension: 0.4
            }))
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: true },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `${context.dataset.label}: ${context.parsed.y.toFixed(2)} µg/m³`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: false,
                    title: { display: true, text: 'Değer (µg/m³)' }
                },
                x: {
                    title: { display: true, text: 'Zaman' }
                }
            }
        }
    });
}
```

---

## 🔍 Vector DB RAG (Retrieval Augmented Generation)

### Qdrant ile Analiz Kaydetme

**ÖNEMLİ:** MCP Server otomatik olarak analiz sonuçlarını Qdrant'a kaydeder. Ancak manuel kayıt için:

```csharp
public async Task<string> SaveAnalysisToVectorDbAsync(
    string tenantSlug,
    string analysisText,
    string analysisType = "analysis",
    Dictionary<string, object> metadata = null)
{
    // MCP Server'a istek gönder (save_analysis_to_vector_db tool)
    var result = await _mcpClient.CallToolAsync("save_analysis_to_vector_db", new
    {
        tenant_slug = tenantSlug,
        analysis_text = analysisText,
        analysis_type = analysisType,
        metadata = metadata ?? new Dictionary<string, object>()
    });
    
    // MCP Server:
    // 1. sentence-transformers ile embedding oluşturur (384 dimensions)
    // 2. Qdrant'ın tenant_{slug} collection'ına kaydeder
    // 3. Vector ID döndürür
    
    return result.VectorId;
}
```

### Qdrant ile Analiz Arama (Semantic Search)

```csharp
public async Task<List<AnalysisSearchResult>> SearchAnalysisFromVectorDbAsync(
    string tenantSlug,
    string queryText,
    int limit = 5,
    double scoreThreshold = 0.5,
    string filterType = null)
{
    // MCP Server'a istek gönder (search_analysis_from_vector_db tool)
    var result = await _mcpClient.CallToolAsync("search_analysis_from_vector_db", new
    {
        tenant_slug = tenantSlug,
        query_text = queryText,
        limit = limit,
        score_threshold = scoreThreshold,
        filter_type = filterType
    });
    
    // MCP Server:
    // 1. Query metnini embedding'e dönüştürür
    // 2. Qdrant'ın tenant_{slug} collection'ında semantic search yapar
    // 3. Cosine similarity ile en benzer analizleri bulur
    // 4. Score threshold'u geçen sonuçları döndürür
    
    return result.Results.Select(r => new AnalysisSearchResult
    {
        AnalysisText = r.Payload["text"],
        AnalysisType = r.Payload.GetValueOrDefault("analysis_type", "unknown"),
        Score = r.Score,
        Metadata = r.Payload
    }).ToList();
}
```

### Embedding Model Detayları

```python
# embedding_utils.py
Model: paraphrase-multilingual-MiniLM-L12-v2
Dimensions: 384
Language Support: Türkçe dahil çoklu dil
Distance Metric: COSINE
Normalization: L2 normalized embeddings

# Kullanım
from embedding_utils import generate_embedding

embedding = generate_embedding("Akçansa'nın Şubat ayı analizi")
# Returns: List[float] (384 dimensions)
```

### RAG ile Context Enrichment

```csharp
public async Task<string> EnrichContextWithRAGAsync(string userMessage, string tenantSlug)
{
    // 1. Qdrant'tan ilgili analizleri bul (MCP Server üzerinden)
    var relevantAnalyses = await _airQualityMcpService.SearchAnalysisFromVectorDbAsync(
        tenantSlug: tenantSlug,
        queryText: userMessage,
        limit: 3,
        scoreThreshold: 0.6
    );
    
    // 2. Context string'i oluştur
    var contextBuilder = new StringBuilder();
    if (relevantAnalyses.Any())
    {
        contextBuilder.AppendLine("İlgili geçmiş analizler:");
        foreach (var analysis in relevantAnalyses)
        {
            contextBuilder.AppendLine($"- {analysis.AnalysisText.Substring(0, Math.Min(200, analysis.AnalysisText.Length))}...");
            if (analysis.Metadata.ContainsKey("start_date"))
            {
                contextBuilder.AppendLine($"  (Tarih: {analysis.Metadata["start_date"]})");
            }
            contextBuilder.AppendLine($"  (Similarity: {analysis.Score:F3})");
        }
    }
    
    // 3. LLM'e context ile birlikte gönder
    var enrichedPrompt = $@"{contextBuilder.ToString()}

Kullanıcı sorusu: {userMessage}

Yukarıdaki geçmiş analizleri dikkate alarak kullanıcının sorusunu cevapla.";
    
    return enrichedPrompt;
}
```

### Tenant İzolasyonu (Qdrant)

**ÖNEMLİ:** Her tenant'ın ayrı Qdrant collection'ı var:

```python
# Collection naming: tenant_{slug}
# Örnek: tenant_akcansa, tenant_tupras, tenant_bursa-metropolitan-municipality

# Güvenlik katmanları:
# 1. Collection seviyesi: Fiziksel ayrım (tenant_akcansa vs tenant_tupras)
# 2. API seviyesi: Her fonksiyon tenant_slug parametresi alır
# 3. Payload seviyesi: Her vector'da _tenant field'ı var (double-check)
```

**Kurulum:**
```bash
# Vector DB setup (her tenant için collection oluştur)
python3 vector_db_setup.py

# Bu script:
# 1. MongoDB'den tüm tenant'ları alır
# 2. Her tenant için Qdrant collection oluşturur (tenant_{slug})
# 3. Collection'ları 384 dimension, COSINE distance ile yapılandırır
```

---

## 🎨 UI Bileşenleri

### Air Quality Card Component

```razor
@* Components/AirQualityCard.razor *@
<div class="air-quality-card">
    <div class="card-header">
        <h3>@Location</h3>
        <span class="quality-badge quality-@QualityLevel.ToLower()">
            @QualityLevel
        </span>
    </div>
    <div class="card-body">
        <div class="pollutant-list">
            @foreach (var pollutant in Pollutants)
            {
                <div class="pollutant-item">
                    <span class="pollutant-name">@pollutant.Name</span>
                    <span class="pollutant-value">@pollutant.Value @pollutant.Unit</span>
                    <div class="pollutant-bar">
                        <div class="pollutant-fill" 
                             style="width: @(pollutant.Percentage)%">
                        </div>
                    </div>
                </div>
            }
        </div>
    </div>
    <div class="card-footer">
        <span class="timestamp">@Timestamp.ToString("dd.MM.yyyy HH:mm")</span>
        <button class="btn-details" @onclick="ShowDetails">Detaylar</button>
    </div>
</div>
```

### Chart Component

```razor
@* Components/AirQualityChart.razor *@
<div class="chart-container">
    <canvas id="airQualityChart" @ref="_chartCanvas"></canvas>
</div>

@code {
    private ElementReference _chartCanvas;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && ChartData != null)
        {
            await JSRuntime.InvokeVoidAsync("renderAirQualityChart", ChartData);
        }
    }
}
```

---

## 🧪 Test Senaryoları

### 1. Basit Hava Kalitesi Sorgusu

```
Kullanıcı: "İstanbul'da bugünkü PM2.5 değeri nedir?"
Bot: "İstanbul'da bugünkü PM2.5 değeri 45 µg/m³ olarak ölçülmüştür. 
      Hava kalitesi 'Orta' seviyede. Detaylı bilgi için grafiği inceleyebilirsiniz."
[Grafik gösterilir]
```

### 2. Zaman Aralığı Analizi

```
Kullanıcı: "Ankara'da son 7 günün PM10 ortalaması"
Bot: "Ankara'da son 7 günün PM10 ortalaması 62 µg/m³ olarak hesaplanmıştır.
      Bu değer, günlük limit değerin (50 µg/m³) üzerindedir.
      [Grafik gösterilir]
      Detaylı analiz raporu ister misiniz?"
```

### 3. Karşılaştırmalı Analiz

```
Kullanıcı: "Ocak ve Şubat ayları karşılaştırması"
Bot: "Ocak ve Şubat ayları karşılaştırması:
      - Ocak: PM2.5 ortalaması 38 µg/m³
      - Şubat: PM2.5 ortalaması 42 µg/m³
      - Fark: +4 µg/m³ (%10.5 artış)
      [Karşılaştırma grafiği gösterilir]"
```

### 4. RAG ile Geçmiş Analiz Kullanımı

```
Kullanıcı: "Geçen ay yaptığımız analiz ne diyordu?"
Bot: [Vector DB'den geçmiş analizi bulur]
     "Geçen ay yaptığınız analizde, İstanbul'da PM2.5 değerlerinde 
      %15 artış gözlemlenmişti. Şu anki değerlerle karşılaştırmak ister misiniz?"
```

---

## 📈 Performance Optimizasyonları

1. **Caching Strategy**
   - PostgreSQL query sonuçları 5 dakika cache'lenir
   - LLM response'ları 10 dakika cache'lenir
   - MCP tool çağrıları cache'lenebilir
   - Redis kullanılabilir

2. **Database Indexing**
   - PostgreSQL: `{ device_id, calculated_datetime }`, `{ parameter, calculated_datetime }`
   - MongoDB: `{ SlugName: 1 }` (Tenants), `{ TenantSlugName: 1 }` (Devices)
   - Qdrant: Otomatik index (HNSW)

3. **Async Operations**
   - Tüm I/O işlemleri async/await
   - MCP server çağrıları async
   - Paralel query'ler mümkün olduğunca kullanılır

4. **Pagination**
   - Büyük veri setleri için sayfalama
   - Lazy loading grafiklerde
   - Device bazlı batch processing

5. **Tenant Isolation Performance**
   - Her tenant'ın ayrı collection'ı sayesinde query'ler daha hızlı
   - Collection bazlı index'ler optimize edilmiş

---

## 🔐 Güvenlik Best Practices

1. **Input Sanitization**
   - Tüm kullanıcı girdileri sanitize edilir
   - SQL injection koruması (EF Core parameterized queries)
   - NoSQL injection koruması (MongoDB driver)

2. **Authentication & Authorization**
   - JWT token tabanlı auth
   - Role-based access control (RBAC)
   - API key validation per domain

3. **Rate Limiting**
   - IP bazlı rate limiting
   - User bazlı rate limiting
   - Endpoint bazlı rate limiting

4. **Data Privacy**
   - Kişisel veriler şifrelenir
   - GDPR uyumluluğu
   - Data retention policies

---

## 🚀 Deployment

### Docker Compose

```yaml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__PostgreSQL=Host=postgres;Database=airqualitychatbot;...
      - ConnectionStrings__MongoDB=mongodb://mongo:27017
      - ConnectionStrings__PostgreSQLAirQuality=Host=postgres-airquality;Database=airqoon;...
    depends_on:
      - postgres
      - postgres-airquality
      - mongo
      - qdrant
  
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: airqualitychatbot
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
  
  postgres-airquality:
    image: postgres:15
    environment:
      POSTGRES_DB: airqoon
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_airquality_data:/var/lib/postgresql/data
    ports:
      - "5433:5432"  # Farklı port
  
  mongo:
    image: mongo:7
    volumes:
      - mongo_data:/data/db
    ports:
      - "27017:27017"
  
  qdrant:
    image: qdrant/qdrant:latest
    container_name: airqoon-qdrant
    ports:
      - "6333:6333"  # REST API
      - "6334:6334"  # gRPC
    volumes:
      - qdrant_storage:/qdrant/storage
    environment:
      - QDRANT__SERVICE__GRPC_PORT=6334
    restart: unless-stopped

volumes:
  postgres_data:
  postgres_airquality_data:
  mongo_data:
  qdrant_storage:
```

### MCP Server Kurulumu

```bash
# 1. Python virtual environment
python3 -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# 2. Dependencies
pip install -r requirements.txt

# 3. Qdrant collection setup
python3 vector_db_setup.py

# 4. MCP Server test
python3 -c "from mcp_server import *; print('MCP Server OK')"
```

---

## 🔗 Domain -> Tenant Mapping

**ÖNEMLİ:** Mevcut projede domain bazlı çalışma var (örn: `example.com` -> özel görünüm). Airqoon'da ise tenant bazlı çalışma var. Bu iki sistemi birleştirmek için:

### DomainTenantMappings Tablosu

```sql
-- Domain -> Tenant Slug eşleştirmesi
INSERT INTO DomainTenantMappings (Domain, TenantSlug, IsActive) VALUES
('akcansa.com', 'akcansa', true),
('tupras.com', 'tupras', true),
('bursa.bel.tr', 'bursa-metropolitan-municipality', true);
```

### TenantMappingService

```csharp
// Services/ITenantMappingService.cs
public interface ITenantMappingService
{
    Task<string?> GetTenantSlugByDomainAsync(string domain);
    Task<string?> ConvertTenantNameToSlugAsync(string tenantName);
    Task<List<DomainTenantMapping>> GetAllMappingsAsync();
    Task SaveMappingAsync(string domain, string tenantSlug);
}

// Kullanım:
// 1. Embed chat'te domain'den tenant slug al
// 2. ConversationContext'e tenant slug kaydet
// 3. Tüm MCP çağrılarında tenant slug kullan
```

### ChatOrchestrationService'te Kullanım

```csharp
// EnsureSessionAsync içinde
var domain = ExtractDomainFromRequest();
var tenantSlug = await _tenantMappingService.GetTenantSlugByDomainAsync(domain);

var session = new ChatSession
{
    SessionId = sessionId,
    Domain = domain,
    TenantSlug = tenantSlug,  // YENİ ALAN
    // ...
};

// ProcessMessageAsync içinde
var context = await GetOrCreateContextAsync(sessionId);
if (string.IsNullOrEmpty(context.TenantSlug) && !string.IsNullOrEmpty(session.TenantSlug))
{
    context.TenantSlug = session.TenantSlug;
    context.Domain = session.Domain;
    await SaveContextAsync(context);
}
```

## 📝 Önemli Notlar

1. **MCP Entegrasyonu**: Python MCP server (`mcp_server.py`) Cursor IDE ile entegre edilmiş. .NET uygulaması MCP server'a stdio veya HTTP üzerinden bağlanır.

2. **Vector DB**: Qdrant kullanılıyor (MongoDB Atlas Vector değil). Her tenant için ayrı collection (`tenant_{slug}`).

3. **Embedding Model**: `paraphrase-multilingual-MiniLM-L12-v2` kullanılıyor (384 dimensions, Türkçe destekli). OpenAI embedding değil.

4. **Veritabanı Yapısı**:
   - PostgreSQL: `air_quality_index` tablosu (device_id bazlı ölçümler)
   - MongoDB: `airqoonBaseMapDB` database (Tenants, Devices collection'ları)
   - Qdrant: Vector database (tenant bazlı collection'lar)

5. **Parametre Normalizasyonu**: 
   - PM10 → PM10-24h
   - PM2.5 → PM2.5-24h
   - NO2 → NO2-1h
   - O3 → O3-1h
   - SO2 → SO2-1h
   - CO → CO-8h

6. **Tenant İzolasyonu**: 3 katmanlı güvenlik (collection, API, payload seviyesi)

7. **Device-based Filtering**: PostgreSQL sorgularında `device_id = ANY(%s)` kullanılır (tenant'a ait tüm cihazlar)

8. **Caching**: Redis kullanılması önerilir production'da

9. **Monitoring**: Application Insights veya Sentry entegrasyonu önerilir

10. **MCP Server Kurulumu**: Python dependencies (`requirements.txt`) yüklenmeli, Qdrant Docker container'ı çalışmalı

11. **Domain -> Tenant Mapping**: Her domain için bir tenant slug eşleştirmesi yapılmalı. Admin dashboard'dan yönetilebilir.

12. **ConversationContext**: Tenant bilgisi context'te saklanmalı. Domain'den otomatik olarak tenant slug alınabilir.

13. **Tenant Name -> Slug Conversion**: Kullanıcı "Akçansa" dediğinde "akcansa" slug'ına çevrilmeli (MongoDB Tenants collection'ından lookup).

---

## 📦 Model Güncellemeleri

### ConversationContext Model

```csharp
// Models/ConversationContext.cs
public class ConversationContext
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public IntentType? CurrentIntent { get; set; }
    public Dictionary<string, string> CollectedParameters { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    // Tenant & Domain
    public string? TenantSlug { get; set; }
    public string? Domain { get; set; }
    
    // Air Quality Query specific
    public string? Pollutant { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Month1 { get; set; }  // YYYY-MM
    public string? Month2 { get; set; }  // YYYY-MM
    
    // Validation counters
    public int TenantInvalidAttempts { get; set; } = 0;
}
```

### ConversationContextEntity

```csharp
// Data/ConversationContextEntity.cs
public class ConversationContextEntity
{
    [Key]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;
    
    [ForeignKey("SessionId")]
    public ChatSession? Session { get; set; }
    
    public string? CurrentIntent { get; set; }
    [Column(TypeName = "text")]
    public string? CollectedParametersJson { get; set; }
    
    // Tenant & Domain
    [MaxLength(255)]
    public string? TenantSlug { get; set; }
    [MaxLength(255)]
    public string? Domain { get; set; }
    
    // Air Quality Query specific
    [MaxLength(50)]
    public string? Pollutant { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(10)]
    public string? Month1 { get; set; }
    [MaxLength(10)]
    public string? Month2 { get; set; }
    
    // Validation counters
    public int TenantInvalidAttempts { get; set; } = 0;
    
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### ChatSession Entity

```csharp
// Data/ChatSession.cs (güncellenmiş)
public class ChatSession
{
    [Key]
    [MaxLength(100)]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(255)]
    public string? Domain { get; set; }
    
    [MaxLength(255)]
    public string? TenantSlug { get; set; }  // YENİ ALAN
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; } = true;
}
```

## 🔌 MCP Client Implementasyonu

### MCP Client Service

```csharp
// Services/IMcpClientService.cs
public interface IMcpClientService
{
    Task<T> CallToolAsync<T>(string toolName, object arguments);
    Task<string> CallToolAsync(string toolName, object arguments);
}

// Services/McpClientService.cs
public class McpClientService : IMcpClientService
{
    private readonly ILogger<McpClientService> _logger;
    private readonly IConfiguration _configuration;
    
    // MCP Server'a stdio veya HTTP üzerinden bağlan
    // Örnek: Process.Start ile Python MCP server'ı başlat
    // veya HTTP endpoint'e istek gönder
    
    public async Task<T> CallToolAsync<T>(string toolName, object arguments)
    {
        // MCP protocol implementasyonu
        // stdio veya HTTP üzerinden tool çağrısı yap
        // JSON response'u parse et ve T'ye dönüştür
    }
}

// Services/AirQualityMcpService.cs
public class AirQualityMcpService : IAirQualityMcpService
{
    private readonly IMcpClientService _mcpClient;
    
    public async Task<TimeRangeAnalysisResult> TenantTimeRangeAnalysisAsync(
        string tenantSlug,
        DateTime startDate,
        DateTime endDate,
        List<string> pollutants = null,
        DateTime? comparisonStartDate = null,
        DateTime? comparisonEndDate = null)
    {
        var result = await _mcpClient.CallToolAsync<TimeRangeAnalysisResult>(
            "tenant_time_range_analysis",
            new
            {
                tenant_slug = tenantSlug,
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                comparison_start_date = comparisonStartDate?.ToString("yyyy-MM-dd"),
                comparison_end_date = comparisonEndDate?.ToString("yyyy-MM-dd"),
                pollutants = pollutants ?? new List<string> { "PM2.5", "PM10", "NO2" }
            });
        
        return result;
    }
    
    // Diğer MCP tool çağrıları...
}
```

## 🎯 Sonuç

Bu prompt, hava kalitesi ölçüm verileri için tam özellikli bir chatbot sistemi oluşturmanız için gereken tüm detayları içermektedir. Mevcut 8BitizChatBot projesindeki yaklaşımlar ve best practice'ler bu projeye adapte edilmiştir.

**Önemli Farklar:**
- Hava kalitesi domain'e özel intent'ler ve parametreler
- PostgreSQL `air_quality_index` tablosu (device_id bazlı)
- MongoDB tenant & device metadata
- Qdrant Vector DB ile RAG implementasyonu (tenant bazlı collection'lar)
- Python MCP server entegrasyonu
- Domain -> Tenant mapping sistemi
- Grafik görselleştirme desteği
- Rapor oluşturma özellikleri

**Korunan Özellikler:**
- Multi-LLM desteği (Ollama, OpenAI, Anthropic)
- Multi-domain embedding (DomainAppearance)
- Admin dashboard (tenant yönetimi ile genişletilmiş)
- Analytics (tenant bazlı filtreleme ile)
- Security features (rate limiting, input validation, spam detection)
- Session management (tenant bilgisi ile)
- Turkish language support
- Conversation context management
- Audit logging

**Yeni Eklenen Özellikler:**
- Tenant bazlı veri izolasyonu
- Domain -> Tenant mapping
- MCP tool entegrasyonu
- Qdrant vector database
- Tenant name -> slug conversion
- Device-based data aggregation
