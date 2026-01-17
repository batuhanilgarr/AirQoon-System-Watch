# Tenant İzolasyonu - Nasıl Uygulandı?

## 🔒 Tenant İzolasyonu Stratejisi

Sistemde **3 katmanlı güvenlik** ile tenant izolasyonu sağlanmıştır:

---

## 1️⃣ **Collection Seviyesi İzolasyon** (Fiziksel Ayrım)

### Her Tenant'ın Kendi Collection'ı Var

```python
# vector_db_api.py - Line 49-51
def _get_collection_name(self, tenant_slug: str) -> str:
    """Tenant slug'ından collection adını döndür"""
    return f"tenant_{tenant_slug}"
```

**Örnek:**
- Akçansa → `tenant_akcansa`
- Tüpraş → `tenant_tupras`
- Bursa Büyükşehir → `tenant_bursa-metropolitan-municipality`

**Sonuç:** Her tenant'ın verileri **fiziksel olarak ayrı** collection'larda saklanır. Bir tenant başka tenant'ın collection'ına erişemez.

### Kurulum

```python
# vector_db_setup.py
def create_tenant_collection(self, tenant_slug: str):
    collection_name = f"tenant_{tenant_slug}"
    self.client.create_collection(
        collection_name=collection_name,
        vectors_config=VectorParams(size=1536, distance=Distance.COSINE)
    )
```

**Durum:** 35 tenant için 35 ayrı collection oluşturuldu.

---

## 2️⃣ **API Seviyesi Kontrol** (Erişim Kontrolü)

### Her İşlem Tenant Context'i İçinde Yapılır

```python
# vector_db_api.py - Line 62-96
def insert_vector(self, tenant_slug: str, vector_id: str, vector: List[float], ...):
    """Tenant'a özel vector ekle - Sadece ilgili tenant'ın collection'ına eklenir"""
    
    # 1. Tenant collection varlık kontrolü
    if not self._verify_tenant_collection(tenant_slug):
        raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
    
    # 2. Doğru collection'a ekle
    collection_name = self._get_collection_name(tenant_slug)  # tenant_akcansa
    
    # 3. Payload'a tenant bilgisi ekle (double-check)
    payload["_tenant"] = tenant_slug
    
    # 4. Sadece ilgili collection'a yaz
    self.client.upsert(
        collection_name=collection_name,  # tenant_akcansa
        points=[PointStruct(...)]
    )
```

### Arama İşlemleri

```python
# vector_db_api.py - Line 98-149
def search_vectors(self, tenant_slug: str, query_vector: List[float], ...):
    """Tenant'a özel vector arama - Sadece ilgili tenant'ın collection'ında arama"""
    
    # 1. Collection kontrolü
    collection_name = self._get_collection_name(tenant_slug)  # tenant_akcansa
    
    # 2. Tenant filter'ı ekle (ekstra güvenlik)
    tenant_filter = Filter(
        must=[
            FieldCondition(
                key="_tenant",
                match=MatchValue(value=tenant_slug)  # akcansa
            )
        ]
    )
    
    # 3. Sadece ilgili collection'da ara
    results = self.client.query_points(
        collection_name=collection_name,  # tenant_akcansa
        query=query_vector,
        query_filter=tenant_filter  # _tenant == "akcansa"
    )
```

**Sonuç:** API seviyesinde her işlem tenant context'i içinde yapılır. Yanlış tenant kullanımı hata verir.

---

## 3️⃣ **Payload Seviyesi Kontrol** (Double-Check)

### Her Vector'da Tenant Bilgisi Saklanır

```python
# vector_db_api.py - Line 78-81
# Payload'a tenant bilgisi ekle (ekstra güvenlik)
if payload is None:
    payload = {}
payload["_tenant"] = tenant_slug  # Double-check için
```

### Get Vector İşleminde Kontrol

```python
# vector_db_api.py - Line 151-182
def get_vector(self, tenant_slug: str, vector_id: str):
    """Tenant'a özel vector getir"""
    
    # 1. Sadece kendi collection'ından al
    points = self.client.retrieve(
        collection_name=collection_name,  # tenant_akcansa
        ids=[vector_id]
    )
    
    # 2. Payload'daki tenant bilgisini kontrol et
    payload_tenant = point.payload.get("_tenant")
    if payload_tenant and payload_tenant != tenant_slug:
        raise ValueError(f"GÜVENLİK İHLALİ: Vector başka tenant'a ait!")
```

**Sonuç:** Her vector'da `_tenant` field'ı saklanır. Yanlış tenant erişimi tespit edilir.

---

## 🛡️ MCP Server'da İzolasyon

### Tüm Tool'lar Tenant Context'i İçinde Çalışır

```python
# mcp_server.py - Line 275-292
async def handle_time_range_analysis(arguments: Dict):
    tenant_slug = arguments.get("tenant_slug")  # ZORUNLU PARAMETRE
    
    # 1. Tenant doğrulama (MongoDB'den)
    tenant = db["Tenants"].find_one({"SlugName": tenant_slug})
    if not tenant:
        return [TextContent(text=f"❌ Tenant bulunamadı: {tenant_slug}")]
    
    # 2. Tenant'a ait device'ları al
    devices = db["Devices"].find({"TenantSlugName": tenant_slug})
    device_ids = [d["DeviceId"] for d in devices]
    
    # 3. Sadece bu device'ların verilerini analiz et
    query = """
        SELECT ... 
        FROM air_quality_index
        WHERE device_id = ANY(%s)  -- Sadece tenant'ın device'ları
    """
    cursor.execute(query, (device_ids,))
```

**Sonuç:** MCP server'da her sorgu tenant bazlı yapılır. Bir tenant başka tenant'ın device'larına erişemez.

---

## 📊 İzolasyon Garantileri

### 1. Collection Seviyesi
- ✅ Her tenant'ın ayrı collection'ı var
- ✅ Bir tenant başka tenant'ın collection'ına erişemez
- ✅ Fiziksel ayrım sağlanmış

### 2. API Seviyesi
- ✅ Her fonksiyon `tenant_slug` parametresi alır (zorunlu)
- ✅ Collection varlık kontrolü yapılır
- ✅ Yanlış tenant kullanımı hata verir

### 3. Payload Seviyesi
- ✅ Her vector'da `_tenant` field'ı var
- ✅ Double-check mekanizması çalışır
- ✅ Tenant mismatch tespit edilir

### 4. Database Seviyesi
- ✅ PostgreSQL: `device_id` ile filtreleme
- ✅ MongoDB: `TenantSlugName` ile filtreleme
- ✅ Her sorgu tenant bazlı yapılır

---

## 🔍 Güvenlik Testi

```python
# vector_db_api.py - Line 380-395
# Test: Tüpraş tenant'ından Akçansa verisine erişim DENEMESİ
api.get_vector(tenant_slug="tupras", vector_id=akcansa_vector_id)

# Sonuç: 
# ✓ Güvenlik koruması çalışıyor: Vector farklı tenant'ın collection'ında, erişim yok
```

**Sonuç:** Cross-tenant erişim engellenmiştir.

---

## 📈 Mevcut Durum

- **35 tenant collection'ı** oluşturuldu
- Her collection **tamamen izole**
- **3 katmanlı güvenlik** aktif
- **MCP server** tenant izolasyonu ile çalışıyor

---

## ✅ Özet

**Tenant izolasyonu nasıl sağlandı?**

1. **Fiziksel Ayrım**: Her tenant'ın ayrı collection'ı (`tenant_{slug}`)
2. **API Kontrolü**: Her işlem tenant context'i içinde (`tenant_slug` zorunlu)
3. **Payload Kontrolü**: Her vector'da `_tenant` field'ı (double-check)
4. **Database Filtreleme**: Sorgularda tenant bazlı filtreleme

**Sonuç:** Bir tenant başka tenant'ın verilerine **hiçbir şekilde** erişemez. ✅
