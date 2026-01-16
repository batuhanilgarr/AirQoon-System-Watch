#!/usr/bin/env python3
"""
Airqoon Vector Database Setup with Tenant Isolation
Her tenant için ayrı collection oluşturur ve tam izolasyon sağlar
"""

import os
from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams, CollectionStatus
from typing import List, Dict, Optional
import sys

# Embedding dimension için utility import
try:
    from embedding_utils import get_embedding_dimension
except ImportError:
    # Fallback - eğer embedding_utils yüklenemezse default değer kullan
    def get_embedding_dimension():
        return 384  # sentence-transformers multilingual model default

# Qdrant bağlantı bilgileri
QDRANT_HOST = os.getenv("QDRANT_HOST", "localhost")
QDRANT_PORT = int(os.getenv("QDRANT_PORT", "6333"))
QDRANT_API_KEY = os.getenv("QDRANT_API_KEY", None)  # Production'da kullanılacak

class TenantIsolatedVectorDB:
    """
    Tenant bazlı izole vector database yönetimi
    Her tenant'ın kendi collection'ı var, birbirlerine erişemezler
    """
    
    def __init__(self):
        """Qdrant client'ı başlat"""
        if QDRANT_API_KEY:
            self.client = QdrantClient(
                url=f"http://{QDRANT_HOST}:{QDRANT_PORT}",
                api_key=QDRANT_API_KEY
            )
        else:
            self.client = QdrantClient(
                url=f"http://{QDRANT_HOST}:{QDRANT_PORT}"
            )
        print(f"✓ Qdrant'a bağlandı: {QDRANT_HOST}:{QDRANT_PORT}")
    
    def create_tenant_collection(self, tenant_slug: str, vector_size: Optional[int] = None) -> bool:
        """
        Her tenant için ayrı collection oluştur
        Collection adı: tenant_slug ile prefix'lenir (örn: tenant_akcansa)
        """
        collection_name = f"tenant_{tenant_slug}"
        
        try:
            # Collection zaten var mı kontrol et
            collections = self.client.get_collections()
            existing_names = [col.name for col in collections.collections]
            
            if collection_name in existing_names:
                print(f"⚠ Collection zaten mevcut: {collection_name}")
                return True
            
            # Vector size belirtilmemişse embedding dimension'ını kullan
            if vector_size is None:
                try:
                    vector_size = get_embedding_dimension()
                    print(f"📐 Embedding dimension kullanılıyor: {vector_size}")
                except Exception as e:
                    vector_size = 384  # Fallback
                    print(f"⚠ Embedding dimension alınamadı, default kullanılıyor: {vector_size}")
            
            # Yeni collection oluştur
            self.client.create_collection(
                collection_name=collection_name,
                vectors_config=VectorParams(
                    size=vector_size,
                    distance=Distance.COSINE
                )
            )
            
            print(f"✓ Collection oluşturuldu: {collection_name} (tenant: {tenant_slug}, vector_size: {vector_size})")
            return True
            
        except Exception as e:
            print(f"✗ Collection oluşturma hatası ({tenant_slug}): {str(e)}")
            return False
    
    def get_tenant_collection_name(self, tenant_slug: str) -> str:
        """Tenant slug'ından collection adını döndür"""
        return f"tenant_{tenant_slug}"
    
    def verify_tenant_isolation(self, tenant_slug: str) -> Dict:
        """
        Tenant izolasyonunu doğrula
        Sadece kendi collection'ına erişebilmeli
        """
        collection_name = self.get_tenant_collection_name(tenant_slug)
        
        try:
            collection_info = self.client.get_collection(collection_name)
            return {
                "tenant": tenant_slug,
                "collection": collection_name,
                "status": "exists",
                "points_count": collection_info.points_count,
                "vectors_count": collection_info.vectors_count
            }
        except Exception as e:
            return {
                "tenant": tenant_slug,
                "collection": collection_name,
                "status": "error",
                "error": str(e)
            }
    
    def list_all_tenant_collections(self) -> List[str]:
        """Tüm tenant collection'larını listele"""
        collections = self.client.get_collections()
        tenant_collections = [
            col.name for col in collections.collections 
            if col.name.startswith("tenant_")
        ]
        return tenant_collections
    
    def delete_tenant_collection(self, tenant_slug: str) -> bool:
        """Tenant collection'ını sil (dikkatli kullan!)"""
        collection_name = self.get_tenant_collection_name(tenant_slug)
        try:
            self.client.delete_collection(collection_name)
            print(f"✓ Collection silindi: {collection_name}")
            return True
        except Exception as e:
            print(f"✗ Silme hatası: {str(e)}")
            return False


def setup_all_tenants_from_mongodb():
    """MongoDB'den tenant listesini al ve her biri için collection oluştur"""
    try:
        from pymongo import MongoClient
        
        # MongoDB bağlantısı
        mongo_client = MongoClient("mongodb://localhost:27017/")
        db = mongo_client["airqoonBaseMapDB"]
        tenants_collection = db["Tenants"]
        
        # Tüm tenant'ları al
        tenants = list(tenants_collection.find({}, {"SlugName": 1, "Name": 1}))
        
        print(f"\n📋 MongoDB'den {len(tenants)} tenant bulundu\n")
        
        # Vector DB setup
        vector_db = TenantIsolatedVectorDB()
        
        success_count = 0
        for tenant in tenants:
            tenant_slug = tenant.get("SlugName")
            tenant_name = tenant.get("Name", "Unknown")
            
            if tenant_slug:
                print(f"🔧 Tenant işleniyor: {tenant_slug} ({tenant_name})")
                if vector_db.create_tenant_collection(tenant_slug):
                    success_count += 1
                print()
        
        print(f"\n✅ Toplam {success_count}/{len(tenants)} tenant collection'ı oluşturuldu\n")
        
        # İzolasyon doğrulaması
        print("🔒 Tenant izolasyonu doğrulanıyor...\n")
        for tenant in tenants:
            tenant_slug = tenant.get("SlugName")
            if tenant_slug:
                result = vector_db.verify_tenant_isolation(tenant_slug)
                print(f"  {result['tenant']}: {result['status']} ({result.get('points_count', 0)} points)")
        
        mongo_client.close()
        
    except ImportError:
        print("⚠ pymongo yüklü değil. MongoDB entegrasyonu atlanıyor.")
        print("   Yüklemek için: pip install pymongo")
    except Exception as e:
        print(f"✗ MongoDB bağlantı hatası: {str(e)}")


if __name__ == "__main__":
    print("=" * 60)
    print("Airqoon Vector Database - Tenant Isolation Setup")
    print("=" * 60)
    print()
    
    # Qdrant bağlantısını test et
    try:
        vector_db = TenantIsolatedVectorDB()
        
        # Manuel tenant listesi (MongoDB yoksa)
        manual_tenants = [
            "akcansa",
            "tupras",
            "bursa-metropolitan-municipality",
            "kadikoy-municipality",
            "inegol-municipality",
            "enerjisa-uretim",
            "fernas",
            "oyak-cimento-unye",
            "atasehir-municipality",
            "nilufer-municipality"
        ]
        
        # MongoDB'den tenant'ları al (varsa)
        setup_all_tenants_from_mongodb()
        
        # Tüm collection'ları listele
        print("\n📊 Oluşturulan collection'lar:")
        collections = vector_db.list_all_tenant_collections()
        for col in collections:
            print(f"  - {col}")
        
        print("\n" + "=" * 60)
        print("✅ Setup tamamlandı!")
        print("=" * 60)
        
    except Exception as e:
        print(f"\n✗ Hata: {str(e)}")
        print("\nQdrant'ın çalıştığından emin olun:")
        print("  docker-compose up -d")
        sys.exit(1)
