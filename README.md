# AirQoon System Watch

🌍 **Hava Kalitesi İzleme ve Analiz Platformu** - MCP (Model Context Protocol) tabanlı akıllı hava kalitesi veri analiz sistemi.

AirQoon System Watch, hava kalitesi verilerini analiz etmek, tenant bazlı karşılaştırmalar yapmak ve semantic search ile akıllı sorgular gerçekleştirmek için tasarlanmış güçlü bir MCP server'dır.

## ✨ Özellikler

- 🔍 **Zaman Aralığı Analizi**: Tenant bazlı hava kalitesi verilerinin belirli tarih aralıklarında detaylı analizi
- 📊 **Aylık Karşılaştırma**: İki ay arasındaki hava kalitesi değişikliklerini tespit etme
- 🏢 **Multi-Tenant İzolasyon**: Her tenant'ın verilerine güvenli ve izole erişim
- 🔎 **RAG (Retrieval-Augmented Generation)**: Vector database ile semantic search ve akıllı analiz sorguları
- 📈 **Dramatik Değişiklik Tespiti**: %20'den fazla değişimleri otomatik olarak vurgulama
- 🌐 **Çoklu Veritabanı Desteği**: PostgreSQL, MongoDB ve Qdrant entegrasyonu
- 💬 **Modern Web UI (Blazor)**: Dashboard + Chat sayfası
- 🧠 **Chat UX**: Kalıcı session (localStorage), 30dk inactivity timeout, konuşmayı bitir + geçmiş indirme
- 📝 **Markdown Chat Render**: Asistan yanıtları markdown olarak render edilir (başlıklar/listeler/kod blokları)
- 🧩 **RAG UI**: 2+ sonuç varsa "İlgili Analizleri Göster" ile collapse/expand, 1 sonuç varsa direkt gösterim
- 📅 **Göreli Tarih Sorguları**: "dün", "bugün", "son gün" gibi ifadeler desteklenir

## 🏗️ Mimari

```
┌─────────────────┐
│   MCP Server    │
│  (mcp_server.py)│
└────────┬────────┘
         │
    ┌────┴──────────────────┐
    │                       │
┌───▼────┐  ┌────▼────┐  ┌──▼────┐
│PostgreSQL│ │ MongoDB │ │Qdrant │
│ (Measurements)│ (Tenants/Devices)│ │(Vector DB)│
└─────────┘  └─────────┘  └───────┘
```

## 🚀 Hızlı Başlangıç

### Gereksinimler

- Python 3.8+
- Docker & Docker Compose
- PostgreSQL (localhost:5432)
- MongoDB (localhost:27017)

### Kurulum

1. **Repository'yi klonlayın:**
```bash
git clone git@github.com:batuhanilgarr/AirQoon-System-Watch.git
cd AirQoon-System-Watch
```

2. **Virtual environment oluşturun ve aktif edin:**
```bash
python3 -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
```

3. **Bağımlılıkları yükleyin:**
```bash
pip install -r requirements.txt
```

4. **Docker servislerini başlatın (Qdrant):**
```bash
docker-compose up -d
```

### Web Uygulaması (Dashboard + Chat)

Docker Compose ile `web` servisi ayağa kalktığında:

- Dashboard: `http://localhost:8081/`
- Chat: `http://localhost:8081/chat`
- Admin Tenants: `http://localhost:8081/admin/tenants`
- Admin Analytics: `http://localhost:8081/admin/analytics`
- Healthcheck: `http://localhost:8081/healthz`

Admin notları:

- `/admin/tenants` sayfasında `Device Count`, `Last Activity`, `Status` kolonları vardır.
- `Status`: son 7 gün içinde data varsa **Active**, yoksa **Inactive**.
- Performans için Postgres last-activity sorgusu batch alınır ve kısa süreli memory-cache (2 dk sliding) kullanılır.

### ⚙️ Environment Variables (Docker Compose)

`docker-compose.yml` dosyası varsayılan değerlerle çalışır; production için aşağıdaki değişkenleri tanımlaman önerilir.

- **POSTGRES_USER**
- **POSTGRES_PASSWORD**
- **POSTGRES_DB**
- **POSTGRES_PORT** (varsayılan: 5432, sadece `127.0.0.1` üzerinden publish edilir)
- **MONGO_PORT** (varsayılan: 27017, sadece `127.0.0.1` üzerinden publish edilir)
- **QDRANT_PORT** (varsayılan: 6333, sadece `127.0.0.1` üzerinden publish edilir)
- **QDRANT_GRPC_PORT** (varsayılan: 6334, sadece `127.0.0.1` üzerinden publish edilir)
- **MCP_PORT** (varsayılan: 5005, sadece `127.0.0.1` üzerinden publish edilir)
- **WEB_PORT** (varsayılan: 8080)
- **ASPNETCORE_ENVIRONMENT** (varsayılan: Production)

Opsiyonel override'lar:

- **CONNECTIONSTRINGS__DEFAULTCONNECTION**
- **CONNECTIONSTRINGS__AIRQUALITYCONNECTION**
- **MONGO__CONNECTIONSTRING**
- **MONGO__DATABASE**
- **QDRANT__HOST**
- **MCP__HTTPBASEURL**

Notlar:

- `web` container healthcheck için `GET /healthz` endpoint'i kullanır.
- DB portları varsayılan olarak dış ağa açılmaz (localhost bind). Dışarı açman gerekiyorsa compose mapping'lerini değiştir.

5. **Vector Database'i kurun:**
```bash
python3 vector_db_setup.py
```

Bu komut MongoDB'deki tüm tenant'lar için Qdrant collection'larını oluşturur.

### MCP Server Konfigürasyonu

Cursor IDE için `mcp_config.json` dosyası:

```json
{
  "mcpServers": {
    "airqoon-analyzer": {
      "command": "python3",
      "args": ["/path/to/AirQoon-System-Watch/mcp_server.py"],
      "env": {
        "PGUSER": "your_postgres_user",
        "PYTHONPATH": "/path/to/AirQoon-System-Watch"
      }
    }
  }
}
```

## 📚 Kullanım

### MCP Tools

#### 1. `tenant_time_range_analysis`
Belirli bir tarih aralığında hava kalitesi analizi yapar.

**Parametreler:**
- `tenant_slug`: Tenant slug (örn: "akcansa", "bursa-metropolitan-municipality")
- `start_date`: Başlangıç tarihi (YYYY-MM-DD)
- `end_date`: Bitiş tarihi (YYYY-MM-DD)
- `comparison_start_date` (opsiyonel): Karşılaştırma için başlangıç tarihi
- `comparison_end_date` (opsiyonel): Karşılaştırma için bitiş tarihi
- `pollutants` (opsiyonel): Analiz edilecek kirleticiler (varsayılan: PM2.5, PM10, NO2)

**Örnek:**
```
Akçansa'nın 2025-02-01 ile 2025-04-30 arasındaki verilerini analiz et
```

#### 2. `tenant_monthly_comparison`
İki ay arasındaki dramatik değişiklikleri analiz eder.

**Parametreler:**
- `tenant_slug`: Tenant slug
- `month1`: İlk ay (YYYY-MM)
- `month2`: İkinci ay (YYYY-MM)
- `year` (opsiyonel): Yıl (belirtilmezse her ayın kendi yılı kullanılır)

**Örnek:**
```
Akçansa'nın Şubat 2025 ve Nisan 2025 ayları arasındaki farkları analiz et
```

#### 3. `tenant_statistics`
Tenant'ın genel istatistiklerini gösterir.

**Parametreler:**
- `tenant_slug`: Tenant slug

**Çıktı:**
- Cihaz sayısı
- Vector DB'deki analiz sayısı
- Public/Private durumu

#### 4. `tenant_device_list`
Tenant'a ait tüm cihazları listeler.

**Parametreler:**
- `tenant_slug`: Tenant slug

#### 5. `search_analysis_from_vector_db`
RAG ile semantic search yapar.

**Parametreler:**
- `tenant_slug`: Tenant slug
- `query_text`: Arama sorgusu (Türkçe destekli)
- `limit` (opsiyonel): Maksimum sonuç sayısı (varsayılan: 5)
- `score_threshold` (opsiyonel): Minimum similarity score (varsayılan: 0.5)
- `filter_type` (opsiyonel): Analiz tipi filtresi

**Örnek Sorular:**
- "PM10 değerlerindeki değişiklikler neler?"
- "Hangi aylarda hava kalitesi iyileşti?"
- "Ozon seviyelerinde dramatik değişiklik olan analizler neler?"

#### 6. `save_analysis_to_vector_db`
Manuel olarak analiz sonuçlarını vector DB'ye kaydeder.

## 📊 Veri Kaynakları

- **PostgreSQL**: Hava kalitesi ölçüm verileri (`air_quality_index` tablosu)
- **MongoDB**: Tenant ve cihaz bilgileri (`airqoonBaseMapDB` database)
  - `Tenants`: Tenant bilgileri (SlugName, Name, IsPublic)
  - `Devices`: Cihaz bilgileri (DeviceId, TenantSlugName, Label)
- **Qdrant**: Vector embeddings (semantic search için)
  - Her tenant için ayrı collection: `tenant_{tenant_slug}`
  - Embedding model: `paraphrase-multilingual-MiniLM-L12-v2` (384 dimension)
  - Not: `save_analysis_to_vector_db` artık her kayıtta UUID tabanlı yeni point id üretir (points_count artar)

## 🔐 Güvenlik ve İzolasyon

- **Tenant Isolation**: Her tenant sadece kendi verilerine erişebilir
- **Vector DB İzolasyonu**: Her tenant'ın kendi Qdrant collection'ı var
- **Veritabanı Filtreleri**: Tüm sorgularda tenant bazlı filtreleme yapılır

## 🛠️ Teknoloji Stack

- **Python 3.8+**
- **MCP (Model Context Protocol)**: Cursor IDE entegrasyonu
- **PostgreSQL**: İlişkisel veritabanı (ölçüm verileri)
- **MongoDB**: NoSQL veritabanı (metadata)
- **Qdrant**: Vector database (semantic search)
- **sentence-transformers**: Embedding generation (Türkçe destekli)

## 📁 Proje Yapısı

```
AirQoon-System-Watch/
├── mcp_server.py          # Ana MCP server
├── vector_db_api.py       # Qdrant API wrapper
├── vector_db_setup.py     # Qdrant collection setup
├── embedding_utils.py     # Embedding generation utilities
├── requirements.txt       # Python bağımlılıkları
├── docker-compose.yml     # Qdrant container config
├── mcp_config.json        # MCP server config örneği
└── README.md             # Bu dosya
```

## 🧪 Test

```bash
# MCP server test
python3 -c "from mcp_server import *; print('MCP Server OK')"

# Embedding test
python3 -c "from embedding_utils import generate_embedding; print('Embedding OK')"

# Vector DB test
python3 vector_db_setup.py
```

## 🚢 Production Deployment Notları

- `POSTGRES_PASSWORD` gibi credential'ları production'da repo içine yazma. Environment üzerinden ver veya secret mekanizması kullan.
- PostgreSQL ve MongoDB şimdilik **local** çalıştırılır. Docker Compose yalnızca `qdrant`, `mcp`, `web` servislerini ayağa kaldırır.
- Container'ların host üzerindeki DB'lere erişimi için `host.docker.internal` kullanılır.
- `web` servisinin sağlıklı olduğunu doğrulamak için `GET /healthz` endpoint'i kullanılabilir.
- Production'da ters proxy (örn. Nginx/Caddy) arkasında HTTPS terminasyonu önerilir.

### API Quick Test

```bash
curl -s -X POST http://localhost:8081/api/chat \
  -H 'Content-Type: application/json' \
  -d '{"sessionId":"demo","message":"akcansa icin 2025-01-01 ile 2025-01-08 arasi PM10 analizi","domain":"http://localhost:8081/chat","tenantSlug":"akcansa"}'
```

### Dashboard Chart.js Troubleshooting

Grafikler görünmüyorsa tarayıcı console'da hızlı kontrol:

```js
typeof Chart
window.airqoonCharts
window.airqoonCharts.count()
[...document.querySelectorAll('canvas[id^="aqc_"]')].map(c => ({ id: c.id, w: c.clientWidth, h: c.clientHeight, rendered: c.dataset.aqRendered }))
```

Çalıştırma:

```bash
docker-compose up -d --build
```

## 📝 Örnek Kullanım Senaryoları

### Senaryo 1: Aylık Karşılaştırma
```
Kullanıcı: "Denizli Büyükşehir Belediyesi için son 2 ay arasındaki hava kalitesi değişikliklerini analiz et"

Sistem: 
- Aralık 2025 ve Ocak 2026 verilerini karşılaştırır
- PM10, PM2.5, NO2, O3 parametrelerini analiz eder
- Dramatik değişiklikleri (%20+ değişim) vurgular
- Sonuçları vector DB'ye kaydeder
```

### Senaryo 2: Tenant Karşılaştırması
```
Kullanıcı: "Akçansa ve Bursa arasında hava kalitesi farkları neler?"

Sistem:
- Her iki tenant için aynı tarih aralığında analiz yapar
- Parametreleri karşılaştırır ve farkları hesaplar
- Detaylı karşılaştırma raporu oluşturur
```

### Senaryo 3: Semantic Search
```
Kullanıcı: "PM10 değerlerinde önemli artış olan analizleri bul"

Sistem:
- Vector DB'de semantic search yapar
- Benzer analizleri similarity score'a göre listeler
- İlgili analiz metinlerini döndürür
```

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje özel bir projedir.

## 👤 Yazar

**Batuhan İlgar**

- GitHub: [@batuhanilgarr](https://github.com/batuhanilgarr)

## 🙏 Teşekkürler

- [MCP (Model Context Protocol)](https://modelcontextprotocol.io/)
- [Qdrant](https://qdrant.tech/)
- [sentence-transformers](https://www.sbert.net/)

---

⭐ **Star atarsanız seviniriz!**
