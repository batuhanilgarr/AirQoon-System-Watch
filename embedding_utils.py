#!/usr/bin/env python3
"""
Embedding Utilities - Text to Vector Conversion
Sentence-transformers kullanarak Türkçe metinler için embedding oluşturur
"""

import os
from typing import List, Optional
import hashlib

try:
    from sentence_transformers import SentenceTransformer
except ImportError:
    SentenceTransformer = None

# Global model instance (lazy loading)
_embedding_model = None
_embedding_model_name = "paraphrase-multilingual-MiniLM-L12-v2"  # Türkçe destekleyen model


def get_embedding_model():
    """Embedding model'ini yükle (singleton)"""
    global _embedding_model
    
    if SentenceTransformer is None:
        raise ImportError(
            "sentence-transformers yüklü değil. Yüklemek için: pip install sentence-transformers"
        )
    
    if _embedding_model is None:
        print(f"🔄 Embedding model yükleniyor: {_embedding_model_name}")
        _embedding_model = SentenceTransformer(_embedding_model_name)
        print(f"✓ Model yüklendi (embedding size: {_embedding_model.get_sentence_embedding_dimension()})")
    
    return _embedding_model


def generate_embedding(text: str) -> List[float]:
    """
    Metni embedding vector'üne dönüştür
    
    Args:
        text: Embedding oluşturulacak metin
        
    Returns:
        Embedding vector (List[float])
    """
    model = get_embedding_model()
    embedding = model.encode(text, convert_to_numpy=True, normalize_embeddings=True)
    return embedding.tolist()


def generate_embeddings(texts: List[str], batch_size: int = 32) -> List[List[float]]:
    """
    Birden fazla metni batch olarak embedding vector'üne dönüştür
    
    Args:
        texts: Embedding oluşturulacak metin listesi
        batch_size: Batch size for processing
        
    Returns:
        Embedding vector listesi
    """
    model = get_embedding_model()
    embeddings = model.encode(
        texts, 
        convert_to_numpy=True, 
        normalize_embeddings=True,
        batch_size=batch_size,
        show_progress_bar=len(texts) > 10
    )
    return embeddings.tolist()


def get_embedding_dimension() -> int:
    """Embedding dimension'ını döndür"""
    model = get_embedding_model()
    return model.get_sentence_embedding_dimension()


def generate_vector_id(text: str, prefix: str = "") -> str:
    """
    Metinden unique vector ID oluştur
    
    Args:
        text: ID oluşturulacak metin
        prefix: ID'ye eklenecek prefix (örn: tenant slug)
        
    Returns:
        Unique vector ID (hash-based)
    """
    content = f"{prefix}_{text}" if prefix else text
    vector_id = hashlib.md5(content.encode('utf-8')).hexdigest()
    return vector_id


# Test
if __name__ == "__main__":
    print("🧪 Embedding utility testi...")
    
    try:
        # Test embedding generation
        test_text = "Akçansa'nın Şubat 2025 ve Nisan 2025 arasındaki hava kalitesi analizi"
        embedding = generate_embedding(test_text)
        print(f"✓ Embedding oluşturuldu: {len(embedding)} boyutlu")
        print(f"✓ İlk 5 değer: {embedding[:5]}")
        
        # Test batch embedding
        test_texts = [
            "PM10 değerleri Şubat ayında yüksekti",
            "PM2.5 değerleri Nisan ayında düşüktü",
            "NO2 seviyeleri karşılaştırıldı"
        ]
        embeddings = generate_embeddings(test_texts)
        print(f"✓ Batch embedding oluşturuldu: {len(embeddings)} adet")
        
        # Test dimension
        dim = get_embedding_dimension()
        print(f"✓ Embedding dimension: {dim}")
        
        # Test vector ID generation
        vector_id = generate_vector_id(test_text, prefix="akcansa")
        print(f"✓ Vector ID oluşturuldu: {vector_id}")
        
    except ImportError as e:
        print(f"❌ Hata: {e}")
        print("\nÇözüm:")
        print("  pip install sentence-transformers")
