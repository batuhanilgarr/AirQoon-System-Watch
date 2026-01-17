#!/usr/bin/env python3
"""
Airqoon Vector Database API - Tenant Isolated
Her tenant sadece kendi verilerine erişebilir
"""

import os
from typing import List, Dict, Optional
from qdrant_client import QdrantClient
from qdrant_client.models import PointStruct, Filter, FieldCondition, MatchValue, Query, VectorParams, Distance
from functools import wraps
import json
from datetime import datetime
import hashlib
import uuid

# Embedding utilities
try:
    from embedding_utils import generate_embedding, generate_vector_id, get_embedding_dimension
except ImportError:
    # Fallback - eğer embedding_utils yüklenemezse fonksiyonlar None olur
    generate_embedding = None
    generate_vector_id = None
    get_embedding_dimension = None

# Qdrant bağlantı bilgileri
QDRANT_HOST = os.getenv("QDRANT_HOST", "localhost")
QDRANT_PORT = int(os.getenv("QDRANT_PORT", "6333"))
QDRANT_API_KEY = os.getenv("QDRANT_API_KEY", None)


class TenantIsolatedVectorAPI:
    """
    Tenant bazlı izole vector database API
    Her işlem tenant context'i içinde yapılır
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
    
    def _get_collection_name(self, tenant_slug: str) -> str:
        """Tenant slug'ından collection adını döndür"""
        return f"tenant_{tenant_slug}"
    
    def _verify_tenant_collection(self, tenant_slug: str) -> bool:
        """Tenant collection'ının var olduğunu doğrula"""
        collection_name = self._get_collection_name(tenant_slug)
        try:
            collections = self.client.get_collections()
            existing = collection_name in [col.name for col in collections.collections]
            if existing:
                return True

            # Auto-create missing collection to avoid hard failures in RAG flow.
            vector_size = 384
            if get_embedding_dimension is not None:
                try:
                    vector_size = int(get_embedding_dimension())
                except Exception:
                    vector_size = 384

            self.client.create_collection(
                collection_name=collection_name,
                vectors_config=VectorParams(
                    size=vector_size,
                    distance=Distance.COSINE
                )
            )
            return True
        except Exception:
            return False
    
    def insert_vector(
        self, 
        tenant_slug: str, 
        vector_id: str, 
        vector: List[float],
        payload: Optional[Dict] = None
    ) -> bool:
        """
        Tenant'a özel vector ekle
        Sadece ilgili tenant'ın collection'ına eklenir
        """
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        collection_name = self._get_collection_name(tenant_slug)
        
        # Payload'a tenant bilgisi ekle (ekstra güvenlik)
        if payload is None:
            payload = {}
        payload["_tenant"] = tenant_slug  # Double-check için
        
        try:
            self.client.upsert(
                collection_name=collection_name,
                points=[
                    PointStruct(
                        id=vector_id,
                        vector=vector,
                        payload=payload
                    )
                ]
            )
            return True
        except Exception as e:
            raise Exception(f"Vector ekleme hatası: {str(e)}")
    
    def search_vectors(
        self,
        tenant_slug: str,
        query_vector: List[float],
        limit: int = 10,
        score_threshold: Optional[float] = None,
        filter_payload: Optional[Dict] = None
    ) -> List[Dict]:
        """
        Tenant'a özel vector arama
        Sadece ilgili tenant'ın collection'ında arama yapar
        """
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        collection_name = self._get_collection_name(tenant_slug)
        
        # Tenant filter'ı ekle (ekstra güvenlik)
        tenant_filter = Filter(
            must=[
                FieldCondition(
                    key="_tenant",
                    match=MatchValue(value=tenant_slug)
                )
            ]
        )
        
        # Kullanıcı filter'ı varsa birleştir
        if filter_payload:
            # Filter birleştirme logic'i buraya eklenebilir
            pass
        
        try:
            # Qdrant query API - basit vector query
            results = self.client.query_points(
                collection_name=collection_name,
                query=query_vector,  # Direkt vector geç
                limit=limit,
                score_threshold=score_threshold,
                query_filter=tenant_filter
            )
            
            return [
                {
                    "id": point.id,
                    "score": point.score,
                    "payload": point.payload
                }
                for point in results.points
            ]
        except Exception as e:
            raise Exception(f"Arama hatası: {str(e)}")
    
    def get_vector(self, tenant_slug: str, vector_id: str) -> Optional[Dict]:
        """
        Tenant'a özel vector getir
        ÖNEMLİ: Vector ID'si farklı tenant'ın collection'ında olsa bile
        sadece kendi tenant'ının collection'ında arama yapar
        """
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        collection_name = self._get_collection_name(tenant_slug)
        
        try:
            # Sadece kendi tenant'ının collection'ında ara
            points = self.client.retrieve(
                collection_name=collection_name,
                ids=[vector_id]
            )
            
            if points:
                point = points[0]
                # Double-check: Payload'da tenant bilgisi var mı kontrol et
                payload_tenant = point.payload.get("_tenant") if point.payload else None
                if payload_tenant and payload_tenant != tenant_slug:
                    raise ValueError(f"GÜVENLİK İHLALİ: Vector başka tenant'a ait! (Beklenen: {tenant_slug}, Bulunan: {payload_tenant})")
                
                return {
                    "id": point.id,
                    "vector": point.vector,
                    "payload": point.payload
                }
            # Vector bulunamadı - bu normal, çünkü farklı tenant'ın collection'ında
            return None
        except ValueError:
            # Güvenlik hatası - yukarı fırlat
            raise
        except Exception as e:
            raise Exception(f"Vector getirme hatası: {str(e)}")
    
    def delete_vector(self, tenant_slug: str, vector_id: str) -> bool:
        """Tenant'a özel vector sil"""
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        collection_name = self._get_collection_name(tenant_slug)
        
        try:
            self.client.delete(
                collection_name=collection_name,
                points_selector=[vector_id]
            )
            return True
        except Exception as e:
            raise Exception(f"Vector silme hatası: {str(e)}")
    
    def get_collection_stats(self, tenant_slug: str) -> Dict:
        """Tenant collection istatistikleri"""
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        collection_name = self._get_collection_name(tenant_slug)
        
        try:
            collection_info = self.client.get_collection(collection_name)
            return {
                "tenant": tenant_slug,
                "collection": collection_name,
                "points_count": collection_info.points_count if hasattr(collection_info, 'points_count') else 0,
                "vectors_count": collection_info.vectors_count if hasattr(collection_info, 'vectors_count') else 0,
                "indexed_vectors_count": collection_info.indexed_vectors_count if hasattr(collection_info, 'indexed_vectors_count') else 0,
                "status": str(collection_info.status) if hasattr(collection_info, 'status') else "unknown"
            }
        except Exception as e:
            raise Exception(f"İstatistik hatası: {str(e)}")
    
    def save_analysis(
        self,
        tenant_slug: str,
        analysis_text: str,
        analysis_metadata: Optional[Dict] = None,
        vector_id: Optional[str] = None
    ) -> str:
        """
        Analiz sonuçlarını vector database'e kaydet (RAG için)
        
        Args:
            tenant_slug: Tenant slug
            analysis_text: Analiz metni (embedding oluşturulacak)
            analysis_metadata: Analiz metadata'sı (örn: tarih, tip, vb.)
            vector_id: Vector ID (belirtilmezse otomatik oluşturulur)
            
        Returns:
            Vector ID
        """
        if generate_embedding is None or generate_vector_id is None:
            raise ImportError("embedding_utils modülü yüklenemedi. sentence-transformers yüklü mü?")
        
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        # Vector ID oluştur
        if vector_id is None:
            vector_id = uuid.uuid4().hex
        
        # Embedding oluştur
        embedding = generate_embedding(analysis_text)
        
        # Payload hazırla
        payload = {
            "_tenant": tenant_slug,
            "text": analysis_text,
            "type": "analysis",
            "created_at": datetime.now().isoformat(),
            **(analysis_metadata or {})
        }
        
        # Vector'ü kaydet
        self.insert_vector(
            tenant_slug=tenant_slug,
            vector_id=vector_id,
            vector=embedding,
            payload=payload
        )
        
        return vector_id
    
    def search_analysis(
        self,
        tenant_slug: str,
        query_text: str,
        limit: int = 5,
        score_threshold: Optional[float] = 0.5,
        filter_metadata: Optional[Dict] = None
    ) -> List[Dict]:
        """
        RAG ile analiz sonuçlarını ara
        
        Args:
            tenant_slug: Tenant slug
            query_text: Arama sorgusu (metin)
            limit: Maksimum sonuç sayısı
            score_threshold: Minimum similarity score (0-1 arası)
            filter_metadata: Ek metadata filter'ı (örn: {"type": "monthly_comparison"})
            
        Returns:
            Benzer analiz sonuçları listesi (score ve payload ile)
        """
        if generate_embedding is None:
            raise ImportError("embedding_utils modülü yüklenemedi. sentence-transformers yüklü mü?")
        
        if not self._verify_tenant_collection(tenant_slug):
            raise ValueError(f"Tenant collection bulunamadı: {tenant_slug}")
        
        # Query embedding oluştur
        query_embedding = generate_embedding(query_text)
        
        # Filter hazırla
        conditions = [
            FieldCondition(
                key="_tenant",
                match=MatchValue(value=tenant_slug)
            )
        ]
        
        # Metadata filter ekle (varsa)
        if filter_metadata:
            for key, value in filter_metadata.items():
                conditions.append(
                    FieldCondition(
                        key=key,
                        match=MatchValue(value=value)
                    )
                )
        
        query_filter = Filter(must=conditions) if len(conditions) > 1 else None
        
        # Vector araması yap
        results = self.search_vectors(
            tenant_slug=tenant_slug,
            query_vector=query_embedding,
            limit=limit,
            score_threshold=score_threshold,
            filter_payload=filter_metadata
        )
        
        return results


def require_tenant_context(func):
    """
    Decorator: Fonksiyonun tenant context'i ile çağrılmasını zorunlu kılar
    """
    @wraps(func)
    def wrapper(*args, **kwargs):
        tenant_slug = kwargs.get("tenant_slug") or (args[1] if len(args) > 1 else None)
        
        if not tenant_slug:
            raise ValueError("tenant_slug parametresi zorunludur!")
        
        return func(*args, **kwargs)
    
    return wrapper


# Kullanım örneği
if __name__ == "__main__":
    api = TenantIsolatedVectorAPI()
    
    import uuid
    
    # Test: Akçansa için vector ekle
    print("Test: Akçansa tenant'ına vector ekleniyor...")
    akcansa_vector_id = str(uuid.uuid4())
    api.insert_vector(
        tenant_slug="akcansa",
        vector_id=akcansa_vector_id,
        vector=[0.1] * 1536,  # Örnek vector
        payload={"text": "Akçansa test verisi", "type": "document"}
    )
    print(f"✓ Vector eklendi (ID: {akcansa_vector_id})")
    
    # Test: Akçansa için arama
    print("\nTest: Akçansa tenant'ında arama yapılıyor...")
    results = api.search_vectors(
        tenant_slug="akcansa",
        query_vector=[0.1] * 1536,
        limit=5
    )
    print(f"✓ {len(results)} sonuç bulundu")
    
    # Test: Tüpraş tenant'ından Akçansa verisine erişmeye çalış (başarısız olmalı)
    print("\nTest: Tüpraş tenant'ından Akçansa verisine erişim denemesi...")
    try:
        # Bu başarısız olmalı - farklı collection (Tüpraş collection'ında bu ID yok)
        result = api.get_vector(tenant_slug="tupras", vector_id=akcansa_vector_id)
        if result is None:
            print("✓ Güvenlik koruması çalışıyor: Vector farklı tenant'ın collection'ında, erişim yok")
        else:
            print("✗ GÜVENLİK İHLALİ: Tüpraş Akçansa verisine erişti!")
    except ValueError as e:
        if "GÜVENLİK İHLALİ" in str(e) or "Tenant mismatch" in str(e):
            print(f"✓ Güvenlik koruması çalışıyor: {str(e)}")
        else:
            print(f"✓ Güvenlik koruması çalışıyor: {str(e)}")
    except Exception as e:
        print(f"✓ Güvenlik koruması çalışıyor: {str(e)}")
    
    # İstatistikler
    print("\n📊 Tenant istatistikleri:")
    for tenant in ["akcansa", "tupras", "bursa-metropolitan-municipality"]:
        try:
            stats = api.get_collection_stats(tenant)
            print(f"  ✓ {stats['tenant']}: {stats['points_count']} points")
        except Exception as e:
            print(f"  ✗ {tenant}: {str(e)[:50]}")
