# DEV_NOTES_NEXT.md — Devam Notları (Yarın İçin)

## Hızlı Özet (Bugün Yapılanlar)

- Web UI/UX redesign:
  - Sidebar + dashboard (Home) modernleştirildi
  - Logo entegrasyonu yapıldı (sidebar + hero badge) ve favicon logo olarak güncellendi
- Chat geliştirmeleri:
  - ChatWidget: session persistence (localStorage) + 30dk inactivity timeout + konuşmayı bitir + geçmiş indir
  - ChatWidget: asistan yanıtları markdown olarak render ediliyor (Markdig)
  - RAG UI: 2+ sonuç varsa collapse/expand ("İlgili Analizleri Göster"), 1 sonuçsa direkt göster
  - Göreli tarih sorguları: "dün", "bugün", "son gün" destekleniyor
  - "hava kalitesi" gibi pollutant belirtilmeyen sorgularda StatisticalAnalysis guardrail eklendi
- Qdrant:
  - save_analysis artık UUID id üretiyor, aynı metin overwrite etmesin diye (points_count artıyor)

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

## Yarın İçin Önerilen Devam İşleri

1) RAG içeriğini daha da sadeleştir:
   - 2+ sonuçta sadece Top-1 kısa özet + "Tüm Sonuçları Göster" yaklaşımı
   - Alternatif: RAG’ı ayrı bir tab/accordion bileşeni

2) "son gün" için gerçek "son data günü" yaklaşımı:
   - Postgres’ten tenant cihazları için son `calculated_datetime` max çek
   - O günün [00:00, next-day) aralığını otomatik seç

3) Chat UI polish:
   - Mesaj bubble stili (user/assistant), timestamp gösterimi
   - Auto-scroll ve loading indicator (asistan yanıtı beklerken)

4) Admin sayfaları / dashboard grafikler:
   - Chart.js ile dashboard’a küçük trend grafikleri

## Son Commitler (Referans)

- UI: dashboard + sidebar/chat polish
- Chat: session persistence + timeout + history download
- UI: logo + favicon entegrasyonu
- Chat: service-call (localhost:8081 refused fix)
- Chat UI: markdown rendering + cleaner replies
- Vector DB: UUID point id (Qdrant upsert overwrite fix)
- Chat: collapsible RAG + relative date support
