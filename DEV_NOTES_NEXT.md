# DEV_NOTES_NEXT.md — Devam Notları (Yarın İçin)

## Hızlı Özet (Bugün Yapılanlar - 18 Ocak 2026)

### ✅ UI/UX (Dashboard + Admin) İlerleme (18 Ocak 2026 Akşam)

**1) Admin Tenants: Ek Kolonlar + Details**
- `/admin/tenants` tablosuna eklendi:
  - `Device Count`
  - `Last Activity` (telemetry_averages üzerinden)
  - `Status` (Active/Inactive)
- Status eşiği: **son 7 gün** (`now.AddDays(-7)`)
- Details akışı: `/admin/tenants/{slug}` (TenantDetail sayfası zaten mevcut)

**2) Admin Tenants: Performans (N+1 fix + cache)**
- Önce: tenant başına Postgres last activity sorguları (N+1)
- Sonra:
  - Mongo: tüm device’lar tek seferde çekilip tenant’a göre gruplanıyor
  - Postgres: `GetLatestTelemetryAverageTimestampsAsync(deviceIds, avgTypes)` ile tek batch query
  - `PostgresAirQualityService` içinde `IMemoryCache` ile kısa TTL cache (2 dk sliding)
- Not: İlk istek DB hit; sonraki istekler cache ile çok hızlı.

**3) Dashboard: “Cihazlar (X)” etiketi**
- Dashboard kartlarındaki “Cihazlar (X)” artık listelenen ilk 5 yerine **toplam device count** gösteriyor.

**4) Dashboard: Chart.js render debug/fix (devam ediyor)**
- Chart.js yüklü, canvas var, chart instance oluşuyor (`airqoonCharts.count()=2`) ama görselde görünmeme sorunu araştırıldı.
- `AirQualityChart.razor` JS interop: prerender/timing hatalarında throw etmeden retry edecek şekilde sağlamlaştırıldı.
- `airqoonCharts.js`:
  - `requestAnimationFrame + resize/update` eklendi (multi-line + bar dahil)
  - Debug helper’lar eklendi: `airqoonCharts.count()`, `airqoonCharts.getInfo(...)`
  - `canvas.dataset.aqRendered` flag’i için düzeltmeler yapıldı (Dashboard chart tipleri için).

**Yarın ilk iş (Chart doğrulama):**
- Dashboard `/` aç
- Console:
  - `airqoonCharts.count()`
  - `[...document.querySelectorAll('canvas[id^="aqc_"]')].map(c=>({id:c.id, rendered:c.dataset.aqRendered}))`
  - `canvas.toDataURL().length` ile çizim var mı kontrol et

### ✅ Faz 12 Tamamlandı

**1. MCP Client Service - Health Check ve Error Handling:**
- `IsHealthyAsync()` metodu eklendi (5 saniye timeout)
- Connection refused ve timeout için özel exception handling
- Türkçe kullanıcı dostu hata mesajları
- Graceful degradation - MCP çalışmıyorsa yardımcı mesaj
- `appsettings.json` - timeout ayarları (30 saniye)

**2. UI/UX Düzeltmeleri:**
- Session ID tam görünüm (word-break: break-all)
- Session ID için geliştirilmiş CSS styling
- Chat zaten sayfaya gömülü ve memory sistemi çalışıyor

**3. Telemetry Averages Servis Metodları (WIDE FORMAT):**
- `TelemetryAverage` model oluşturuldu
- `GetTelemetryAveragesAsync()` - önceden hesaplanmış ortalamaları çeker
- `GetTelemetryAverageStatsAsync()` - pollutant bazlı istatistikler
- avgType desteği: '1h', '24h_rolling', '8h_rolling'
- WIDE FORMAT (her pollutant ayrı kolon)

**4. Eval/Doğrulama Sistemi:**
- `ResponseValidationService` oluşturuldu
- Global kısıtlı konular (çevre aktivizmi, politik)
- Tenant bazlı kısıtlamalar (akcansa, tupras)
- Sert ton yumuşatma (tehlikeli → yüksek, kritik → dikkat edilmesi gereken)
- Kısıtlı konuları içeren paragrafları filtreleme

**5. Ortalamaları Prompt'a Otomatik Ekleme:**
- `AverageContextService` oluşturuldu
- Son 30 günlük ortalamaları markdown formatında context olarak hazırlar
- StatisticalAnalysis ve ComparisonAnalysis intent'lerinde otomatik ekleniyor
- 3 saniye timeout koruması

### Önceki Geliştirmeler:
- Web UI/UX redesign (sidebar + dashboard modernleştirildi)
- Logo entegrasyonu (sidebar + hero badge + favicon)
- Chat geliştirmeleri (session persistence, markdown render, RAG UI)
- Göreli tarih sorguları ("dün", "bugün", "son gün")
- Qdrant UUID id üretimi

## Çalıştırma / Sağlık Kontrolü

```bash
# (repo root)
docker compose up -d --build

# health
curl -fsS http://localhost:8081/healthz
curl -fsS http://localhost:5006/healthz

# Qdrant
curl -fsS http://localhost:6333/collections | head -c 400
```

Web:
- Dashboard: http://localhost:8081/
- Chat: http://localhost:8081/chat
- Admin Tenants: http://localhost:8081/admin/tenants
- Admin Analytics: http://localhost:8081/admin/analytics

## Hızlı API Testleri

```bash
# 1) PM10 analizi
curl -s -X POST http://localhost:8081/api/chat \
  -H 'Content-Type: application/json' \
  -d '{"sessionId":"demo","message":"akcansa icin 2025-01-01 ile 2025-01-08 arasi PM10 analizi","domain":"http://localhost:8081/chat","tenantSlug":"akcansa"}'

# 2) Son gün hava kalitesi
curl -s -X POST http://localhost:8081/api/chat \
  -H 'Content-Type: application/json' \
  -d '{"sessionId":"demo","message":"akcansanin son gun hava kalitesini goster","domain":"http://localhost:8081/chat","tenantSlug":"akcansa"}'

# 3) Qdrant count (tenant_akcansa)
curl -fsS -X POST http://localhost:6333/collections/tenant_akcansa/points/count \
  -H 'Content-Type: application/json' \
  -d '{"exact":true}'
```

## Bilinen Notlar / Dikkat Edilecekler

- RAG butonu (collapse/expand) interaktif render’da DOM’a düşer; `curl` ile HTML grep her zaman doğru sonucu vermez.
- "son gün" sorgusu şu an "dün" (yesterday) mantığıyla çalışıyor: `start = today-1`, `end = today`.
- MCP içinde `save_analysis_to_vector_db` çağrıları best-effort; network/timeout olursa chat yanıtı yine gelir.
- Admin Tenants last activity hesaplaması Postgres `telemetry_averages` tablosundan gelir; sayfa performansı için cache vardır.

## Yarın İçin Önerilen Devam İşleri

### Öncelik 1: Faz 12 Test ve Doğrulama
1) **Test Senaryolarını Çalıştır:** ✅
   - MCP health check ve error handling testi ✅
   - Telemetry averages servisi testi (WIDE FORMAT) ✅
   - Eval sistemi testi (kısıtlı konular, sert ton yumuşatma) ✅
   - Ortalamaları context testi (StatisticalAnalysis intent) ✅
   - End-to-end entegrasyon testi ✅
   - Detaylar: `PHASE_12_TESTING_GUIDE.md`

2) **Performans ve Log Kontrolü:** ✅
   - Yanıt süreleri ölçüldü (curl time_total):
     - AirQualityQuery: ~0.56s
     - StatisticalAnalysis (ortalamaları ile): ~0.45s
     - ComparisonAnalysis: ~0.92s
     - MCP Health Check: ~0.004s
   - Log kontrolü yapıldı (kritik hata yok)
   - Not: MCP ilk açılışta embedding warm-up nedeniyle bir süre `healthz=503` dönebilir; readiness sonrası `200` olur.

### Öncelik 2: Opsiyonel İyileştirmeler

1) **Aylık Kayan Ortalama Desteği:**
   - `telemetry_averages` tablosuna 'monthly_rolling' avgType ekle
   - Servis metodlarını güncelle
   - UI'da aylık ortalama seçeneği ekle

2) **Eval Kuralları Veritabanında:**
   - Tenant kurallarını PostgreSQL'de sakla
   - Admin panelinden düzenlenebilir hale getir
   - ResponseValidationService'i dinamik hale getir

3) **Ortalamaları Cache:**
   - Son 30 günlük ortalamalar için MemoryCache ekle
   - Cache invalidation stratejisi (1 saat TTL)
   - Her sorguda yeniden hesaplama önlensin

4) **MCP Retry Mekanizması:**
   - Geçici network hatalarında otomatik retry
   - Exponential backoff stratejisi (1s, 2s, 4s)
   - Max 3 retry

### Öncelik 3: UI/UX İyileştirmeleri

1) **Chat UI Polish:**
   - Mesaj bubble stili (user/assistant ayrımı)
   - Timestamp gösterimi (her mesajda)
   - Auto-scroll (yeni mesaj geldiğinde)
   - Loading indicator (asistan yanıtı beklerken)

2) **RAG İçeriğini Sadeleştir:**
   - 2+ sonuçta sadece Top-1 kısa özet + "Tüm Sonuçları Göster"
   - Alternatif: RAG'ı ayrı bir tab/accordion bileşeni

3) **"Son Gün" için Gerçek Son Data Günü:**
   - Postgres'ten tenant cihazları için son `calculated_datetime` max çek
   - O günün [00:00, next-day) aralığını otomatik seç

4) **Dashboard Grafikler:**
   - Chart.js ile dashboard'a küçük trend grafikleri
   - Son 7 günlük PM10/PM2.5 trend'i
   - Tenant karşılaştırma grafikleri

## Son Commitler (Referans)

- UI: dashboard + sidebar/chat polish
- Chat: session persistence + timeout + history download
- UI: logo + favicon entegrasyonu
- Chat: service-call (localhost:8081 refused fix)
- Chat UI: markdown rendering + cleaner replies
- Vector DB: UUID point id (Qdrant upsert overwrite fix)
- Chat: collapsible RAG + relative date support
