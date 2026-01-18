#!/usr/bin/env python3
"""
Airqoon MCP Server - Tenant Isolated Analysis
Tenant bazlı hava kalitesi analizleri ve zaman aralığı karşılaştırmaları
"""

import asyncio
import os
from datetime import datetime, timedelta
from typing import Any, Optional, List, Dict
try:
    from mcp.server import Server
    from mcp.server.models import InitializationOptions
    from mcp.server.stdio import stdio_server
    from mcp.types import (
        Tool,
        TextContent,
        ImageContent,
        EmbeddedResource,
        LoggingLevel
    )
except ImportError:
    # Fallback for older MCP versions
    from mcp import Server
    from mcp.server.stdio import stdio_server
    from mcp.types import Tool, TextContent
import json

from flask import Flask, request, jsonify
import threading

# Database connections
from pymongo import MongoClient
import psycopg2
from psycopg2.extras import RealDictCursor

# Vector DB
from vector_db_api import TenantIsolatedVectorAPI

# MCP Server instance
server = Server("airqoon-analyzer")

# Database connections (lazy initialization)
mongo_client = None
pg_conn = None
vector_api = None

# HTTP readiness flag (used by /healthz). We treat MCP as "ready" once the embedding model warm-up completes.
_http_ready = threading.Event()

def get_mongo_client():
    """MongoDB client'ı döndür (singleton)"""
    global mongo_client
    if mongo_client is None:
        mongo_uri = os.getenv("MONGO_URI", "mongodb://localhost:27017/")
        mongo_client = MongoClient(mongo_uri)
    return mongo_client

def get_pg_connection():
    """PostgreSQL connection'ı döndür (singleton)"""
    global pg_conn
    if pg_conn is None or pg_conn.closed:
        pg_host = os.getenv("PGHOST", "localhost")
        pg_db = os.getenv("PGDATABASE", "airqoon")
        pg_user = os.getenv("PGUSER", os.getenv("USER", "bhan"))
        pg_password = os.getenv("PGPASSWORD")
        pg_port = int(os.getenv("PGPORT", "5432"))

        pg_conn = psycopg2.connect(
            host=pg_host,
            database=pg_db,
            user=pg_user,
            password=pg_password,
            port=pg_port
        )
    return pg_conn

def get_vector_api():
    """Vector API instance'ı döndür (singleton)"""
    global vector_api
    if vector_api is None:
        vector_api = TenantIsolatedVectorAPI()
    return vector_api


@server.list_tools()
async def list_tools() -> List[Tool]:
    """MCP server'ın sağladığı tool'ları listele"""
    return [
        Tool(
            name="tenant_time_range_analysis",
            description="Tenant'ın belirli bir zaman aralığındaki hava kalitesi verilerini analiz eder. Örnek: Akçansa'nın Şubat ve Nisan ayları arasındaki farklılıkları analiz et.",
            inputSchema={
                "type": "object",
                "properties": {
                    "tenant_slug": {
                        "type": "string",
                        "description": "Tenant slug (örn: 'akcansa', 'tupras', 'bursa-metropolitan-municipality')"
                    },
                    "start_date": {
                        "type": "string",
                        "description": "Başlangıç tarihi (YYYY-MM-DD formatında)"
                    },
                    "end_date": {
                        "type": "string",
                        "description": "Bitiş tarihi (YYYY-MM-DD formatında)"
                    },
                    "comparison_start_date": {
                        "type": "string",
                        "description": "Karşılaştırma için başlangıç tarihi (opsiyonel, YYYY-MM-DD)"
                    },
                    "comparison_end_date": {
                        "type": "string",
                        "description": "Karşılaştırma için bitiş tarihi (opsiyonel, YYYY-MM-DD)"
                    },
                    "pollutants": {
                        "type": "array",
                        "items": {"type": "string"},
                        "description": "Analiz edilecek kirleticiler (örn: ['PM2.5', 'PM10', 'NO2'])"
                    }
                },
                "required": ["tenant_slug", "start_date", "end_date"]
            }
        ),
        Tool(
            name="tenant_monthly_comparison",
            description="İki ay arasındaki dramatik farklılıkları analiz eder. Örnek: Akçansa'nın Şubat ve Nisan ayları arasındaki farklılıklar.",
            inputSchema={
                "type": "object",
                "properties": {
                    "tenant_slug": {
                        "type": "string",
                        "description": "Tenant slug"
                    },
                    "month1": {
                        "type": "string",
                        "description": "İlk ay (YYYY-MM formatında, örn: '2024-02')"
                    },
                    "month2": {
                        "type": "string",
                        "description": "İkinci ay (YYYY-MM formatında, örn: '2024-04')"
                    },
                    "year": {
                        "type": "integer",
                        "description": "Yıl (opsiyonel, belirtilmezse her iki ay için aynı yıl kullanılır)"
                    }
                },
                "required": ["tenant_slug", "month1", "month2"]
            }
        ),
        Tool(
            name="tenant_device_list",
            description="Tenant'a ait cihazları listeler",
            inputSchema={
                "type": "object",
                "properties": {
                    "tenant_slug": {
                        "type": "string",
                        "description": "Tenant slug"
                    }
                },
                "required": ["tenant_slug"]
            }
        ),
        Tool(
            name="tenant_statistics",
            description="Tenant'ın genel istatistiklerini döndürür",
            inputSchema={
                "type": "object",
                "properties": {
                    "tenant_slug": {
                        "type": "string",
                        "description": "Tenant slug"
                    }
                },
                "required": ["tenant_slug"]
            }
        ),
        Tool(
            name="save_analysis_to_vector_db",
            description="Analiz sonuçlarını vector database'e kaydet (RAG için). Analiz metnini embedding'e dönüştürüp vector DB'ye kaydeder.",
            inputSchema={
                "type": "object",
                "properties": {
                    "tenant_slug": {
                        "type": "string",
                        "description": "Tenant slug"
                    },
                    "analysis_text": {
                        "type": "string",
                        "description": "Analiz metni (embedding oluşturulacak)"
                    },
                    "analysis_type": {
                        "type": "string",
                        "description": "Analiz tipi (örn: 'monthly_comparison', 'time_range_analysis')",
                        "default": "analysis"
                    },
                    "metadata": {
                        "type": "object",
                        "description": "Ek metadata (örn: tarih, parametreler, vb.)",
                        "default": {}
                    }
                },
                "required": ["tenant_slug", "analysis_text"]
            }
        ),
        Tool(
            name="search_analysis_from_vector_db",
            description="Vector database'den RAG ile analiz sonuçlarını ara. Metin sorgusu ile semantic search yapar.",
            inputSchema={
                "type": "object",
                "properties": {
                    "tenant_slug": {
                        "type": "string",
                        "description": "Tenant slug"
                    },
                    "query_text": {
                        "type": "string",
                        "description": "Arama sorgusu (örn: 'Şubat Nisan arası PM10 değerleri', 'hava kalitesi iyileşmesi')"
                    },
                    "limit": {
                        "type": "integer",
                        "description": "Maksimum sonuç sayısı",
                        "default": 5
                    },
                    "score_threshold": {
                        "type": "number",
                        "description": "Minimum similarity score (0-1 arası)",
                        "default": 0.5
                    },
                    "filter_type": {
                        "type": "string",
                        "description": "Analiz tipi filter'ı (opsiyonel)",
                        "default": None
                    }
                },
                "required": ["tenant_slug", "query_text"]
            }
        )
    ]


@server.call_tool()
async def call_tool(name: str, arguments: Any) -> List[TextContent]:
    """Tool çağrılarını işle"""
    
    if name == "tenant_time_range_analysis":
        return await handle_time_range_analysis(arguments)
    elif name == "tenant_monthly_comparison":
        return await handle_monthly_comparison(arguments)
    elif name == "tenant_device_list":
        return await handle_device_list(arguments)
    elif name == "tenant_statistics":
        return await handle_tenant_statistics(arguments)
    elif name == "save_analysis_to_vector_db":
        return await handle_save_analysis_to_vector_db(arguments)
    elif name == "search_analysis_from_vector_db":
        return await handle_search_analysis_from_vector_db(arguments)
    else:
        raise ValueError(f"Unknown tool: {name}")


def normalize_pollutant_names(pollutants: List[str]) -> List[str]:
    """
    Parametre isimlerini veritabanı formatına normalize et
    PM10 -> PM10-24h, PM2.5 -> PM2.5-24h, NO2 -> NO2-1h, O3 -> O3-1h, vb.
    """
    normalized = []
    for poll in pollutants:
        poll_upper = poll.upper()
        if poll_upper == "PM10":
            normalized.append("PM10-24h")
        elif poll_upper == "PM2.5" or poll_upper == "PM25":
            normalized.append("PM2.5-24h")
        elif poll_upper == "NO2":
            normalized.append("NO2-1h")
        elif poll_upper == "O3":
            normalized.append("O3-1h")
        elif poll_upper == "SO2":
            normalized.append("SO2-1h")
        elif poll_upper == "CO":
            normalized.append("CO-8h")
        else:
            # Eğer zaten doğru formattaysa olduğu gibi kullan
            normalized.append(poll)
    return normalized


async def handle_time_range_analysis(arguments: Dict) -> List[TextContent]:
    """Zaman aralığı analizi"""
    tenant_slug = arguments.get("tenant_slug")
    start_date = arguments.get("start_date")
    end_date = arguments.get("end_date")
    comparison_start = arguments.get("comparison_start_date")
    comparison_end = arguments.get("comparison_end_date")
    pollutants = arguments.get("pollutants", ["PM2.5", "PM10", "NO2"])
    
    # Parametre isimlerini normalize et
    normalized_pollutants = normalize_pollutant_names(pollutants)
    
    # Tenant doğrulama
    mongo = get_mongo_client()
    db = mongo["airqoonBaseMapDB"]
    tenant = db["Tenants"].find_one({"SlugName": tenant_slug})
    
    if not tenant:
        return [TextContent(
            type="text",
            text=f"❌ Tenant bulunamadı: {tenant_slug}"
        )]
    
    # MongoDB'den tenant'a ait device'ları al
    mongo = get_mongo_client()
    db = mongo["airqoonBaseMapDB"]
    devices = list(db["Devices"].find(
        {"TenantSlugName": tenant_slug},
        {"DeviceId": 1}
    ))
    
    if not devices:
        return [TextContent(
            type="text",
            text=f"⚠️ {tenant_slug} tenant'ına ait cihaz bulunamadı."
        )]
    
    device_ids = [d["DeviceId"] for d in devices]
    
    # PostgreSQL'den veri çek
    conn = get_pg_connection()
    cursor = conn.cursor(cursor_factory=RealDictCursor)
    
    try:
        # Ana zaman aralığı analizi - device_id bazlı
        query = """
            SELECT 
                parameter,
                AVG(concentration) as avg_concentration,
                MIN(concentration) as min_concentration,
                MAX(concentration) as max_concentration,
                COUNT(*) as measurement_count,
                MAX(concentration_unit) as concentration_unit
            FROM air_quality_index
            WHERE device_id = ANY(%s)
                AND calculated_datetime >= %s::timestamp
                AND calculated_datetime < %s::timestamp
                AND parameter = ANY(%s)
            GROUP BY parameter
            ORDER BY parameter;
        """
        
        cursor.execute(query, (device_ids, start_date, end_date, normalized_pollutants))
        main_results = cursor.fetchall()
        
        # Karşılaştırma zaman aralığı (varsa)
        comparison_results = None
        if comparison_start and comparison_end:
            cursor.execute(query, (device_ids, comparison_start, comparison_end, normalized_pollutants))
            comparison_results = cursor.fetchall()
        
        # Sonuçları formatla
        result_text = f"# {tenant.get('Name', tenant_slug)} - Zaman Aralığı Analizi\n\n"
        result_text += f"**Tenant:** {tenant_slug}\n"
        result_text += f"**Analiz Edilen Cihaz Sayısı:** {len(device_ids)}\n"
        result_text += f"**Analiz Tarihi:** {start_date} - {end_date}\n"
        if comparison_start and comparison_end:
            result_text += f"**Karşılaştırma Tarihi:** {comparison_start} - {comparison_end}\n"
        result_text += "\n"
        
        # Ana sonuçlar
        if not main_results:
            result_text += "⚠️ Bu zaman aralığında veri bulunamadı.\n\n"
        else:
            result_text += "## Ana Zaman Aralığı Sonuçları\n\n"
            for row in main_results:
                unit = row.get('concentration_unit') or 'µg/m³'
                result_text += f"### {row['parameter']}\n"
                result_text += f"- Ortalama: {row['avg_concentration']:.2f} {unit}\n"
                result_text += f"- Minimum: {row['min_concentration']:.2f} {unit}\n"
                result_text += f"- Maksimum: {row['max_concentration']:.2f} {unit}\n"
                result_text += f"- Ölçüm Sayısı: {row['measurement_count']}\n\n"
        
        # Karşılaştırma (varsa)
        if comparison_results:
            result_text += "## Karşılaştırma Analizi\n\n"
            # main_results = ilk zaman aralığı (örn: Şubat)
            # comparison_results = ikinci zaman aralığı (örn: Nisan)
            main_dict = {r['parameter']: r for r in main_results}
            
            for comp_row in comparison_results:
                param = comp_row['parameter']
                if param in main_dict:
                    main = main_dict[param]
                    # İkinci zaman aralığı - İlk zaman aralığı (örn: Nisan - Şubat)
                    diff = comp_row['avg_concentration'] - main['avg_concentration']
                    diff_pct = (diff / main['avg_concentration'] * 100) if main['avg_concentration'] > 0 else 0
                    
                    result_text += f"### {param}\n"
                    result_text += f"- **Değişim:** {diff:+.2f} ({diff_pct:+.1f}%)\n"
                    result_text += f"  - Önceki ({start_date}): {main['avg_concentration']:.2f}\n"
                    result_text += f"  - Şimdi ({comparison_start}): {comp_row['avg_concentration']:.2f}\n"
                    
                    if abs(diff_pct) > 20:
                        result_text += f"  - ⚠️ **DRAMATİK DEĞİŞİM TESPİT EDİLDİ!**\n"
                    result_text += "\n"
        
        # Vector DB'ye otomatik kaydet
        vector_api = get_vector_api()
        try:
            analysis_metadata = {
                "analysis_type": "time_range_analysis",
                "start_date": start_date,
                "end_date": end_date,
                "tenant_name": tenant.get('Name', tenant_slug),
                "device_count": len(device_ids)
            }
            # Karşılaştırma tarihlerini sadece varsa ekle
            if comparison_start and comparison_end:
                analysis_metadata["comparison_start_date"] = comparison_start
                analysis_metadata["comparison_end_date"] = comparison_end
            
            vector_id = vector_api.save_analysis(
                tenant_slug=tenant_slug,
                analysis_text=result_text,
                analysis_metadata=analysis_metadata
            )
            result_text += f"\n\n✅ **Analiz vector database'e kaydedildi** (Vector ID: {vector_id})\n"
            result_text += f"Artık RAG ile arama yapabilirsiniz.\n"
        except Exception as e:
            # Vector DB hatası analizi engellemez, sadece uyar
            result_text += f"\n\n⚠️ Vector DB'ye kayıt atlandı: {str(e)[:100]}\n"
        
        return [TextContent(type="text", text=result_text)]
        
    except Exception as e:
        return [TextContent(
            type="text",
            text=f"❌ Hata: {str(e)}"
        )]
    finally:
        cursor.close()


async def handle_monthly_comparison(arguments: Dict) -> List[TextContent]:
    """Aylık karşılaştırma analizi"""
    tenant_slug = arguments.get("tenant_slug")
    month1 = arguments.get("month1")  # YYYY-MM
    month2 = arguments.get("month2")  # YYYY-MM
    year = arguments.get("year")
    
    # Tarihleri parse et
    if year:
        start1 = f"{year}-{month1.split('-')[1]}-01"
        start2 = f"{year}-{month2.split('-')[1]}-01"
    else:
        year1, month1_num = month1.split('-')
        year2, month2_num = month2.split('-')
        start1 = f"{year1}-{month1_num}-01"
        start2 = f"{year2}-{month2_num}-01"
    
    # Ay sonlarını hesapla
    from dateutil.relativedelta import relativedelta
    
    dt1 = datetime.strptime(start1, "%Y-%m-%d")
    dt2 = datetime.strptime(start2, "%Y-%m-%d")
    
    # Bir sonraki ayın ilk günü (exclusive) - bu şekilde ayın son gününün tüm saatleri dahil olur
    next_month1 = dt1 + relativedelta(months=1)
    next_month2 = dt2 + relativedelta(months=1)
    end1 = next_month1.strftime("%Y-%m-%d")  # Bir sonraki ayın ilk günü (exclusive)
    end2 = next_month2.strftime("%Y-%m-%d")  # Bir sonraki ayın ilk günü (exclusive)
    
    # Zaman aralığı analizini kullan
    # Parametre isimleri normalize edilecek (PM10 -> PM10-24h, PM2.5 -> PM2.5-24h, vb.)
    return await handle_time_range_analysis({
        "tenant_slug": tenant_slug,
        "start_date": start1,
        "end_date": end1,
        "comparison_start_date": start2,
        "comparison_end_date": end2,
        "pollutants": ["PM2.5", "PM10", "NO2", "O3"]  # normalize_pollutant_names fonksiyonu bunları dönüştürecek
    })


async def handle_device_list(arguments: Dict) -> List[TextContent]:
    """Tenant'a ait cihazları listele"""
    tenant_slug = arguments.get("tenant_slug")
    
    mongo = get_mongo_client()
    db = mongo["airqoonBaseMapDB"]
    
    devices = list(db["Devices"].find(
        {"TenantSlugName": tenant_slug},
        {"DeviceId": 1, "Name": 1, "Label": 1, "LatestTelemetry": 1}
    ).limit(100))
    
    result_text = f"# {tenant_slug} - Cihaz Listesi\n\n"
    result_text += f"**Toplam Cihaz:** {len(devices)}\n\n"
    
    for device in devices:
        result_text += f"## {device.get('Name', 'Unknown')}\n"
        result_text += f"- Device ID: {device.get('DeviceId')}\n"
        result_text += f"- Label: {device.get('Label', 'N/A')}\n"
        if device.get('LatestTelemetry'):
            result_text += f"- Son Telemetri: Mevcut\n"
        result_text += "\n"
    
    return [TextContent(type="text", text=result_text)]


async def handle_tenant_statistics(arguments: Dict) -> List[TextContent]:
    """Tenant istatistikleri"""
    tenant_slug = arguments.get("tenant_slug")
    
    mongo = get_mongo_client()
    db = mongo["airqoonBaseMapDB"]
    
    tenant = db["Tenants"].find_one({"SlugName": tenant_slug})
    if not tenant:
        return [TextContent(type="text", text=f"❌ Tenant bulunamadı: {tenant_slug}")]
    
    device_count = db["Devices"].count_documents({"TenantSlugName": tenant_slug})
    
    # Vector DB istatistikleri
    vector_api = get_vector_api()
    try:
        vector_stats = vector_api.get_collection_stats(tenant_slug)
        vector_points = vector_stats.get("points_count", 0)
    except:
        vector_points = 0
    
    result_text = f"# {tenant.get('Name', tenant_slug)} - İstatistikler\n\n"
    result_text += f"**Tenant Slug:** {tenant_slug}\n"
    result_text += f"**Cihaz Sayısı:** {device_count}\n"
    result_text += f"**Vector DB Points:** {vector_points}\n"
    result_text += f"**Public:** {'Evet' if tenant.get('IsPublic') else 'Hayır'}\n"
    
    return [TextContent(type="text", text=result_text)]


async def handle_save_analysis_to_vector_db(arguments: Dict) -> List[TextContent]:
    """Analiz sonuçlarını vector database'e kaydet"""
    tenant_slug = arguments.get("tenant_slug")
    analysis_text = arguments.get("analysis_text")
    analysis_type = arguments.get("analysis_type", "analysis")
    metadata = arguments.get("metadata", {})
    
    # Tenant doğrulama
    mongo = get_mongo_client()
    db = mongo["airqoonBaseMapDB"]
    tenant = db["Tenants"].find_one({"SlugName": tenant_slug})
    
    if not tenant:
        return [TextContent(
            type="text",
            text=f"❌ Tenant bulunamadı: {tenant_slug}"
        )]
    
    # Vector DB'ye kaydet
    vector_api = get_vector_api()
    try:
        # Metadata hazırla
        analysis_metadata = {
            "analysis_type": analysis_type,
            **metadata
        }
        
        vector_id = vector_api.save_analysis(
            tenant_slug=tenant_slug,
            analysis_text=analysis_text,
            analysis_metadata=analysis_metadata
        )
        
        result_text = f"# Analiz Vector DB'ye Kaydedildi\n\n"
        result_text += f"**Tenant:** {tenant_slug}\n"
        result_text += f"**Vector ID:** {vector_id}\n"
        result_text += f"**Analiz Tipi:** {analysis_type}\n"
        result_text += f"**Metin Uzunluğu:** {len(analysis_text)} karakter\n"
        result_text += f"\n✅ Analiz başarıyla vector database'e kaydedildi.\n"
        result_text += f"Artık RAG ile arama yapabilirsiniz: `search_analysis_from_vector_db` tool'unu kullanın.\n"
        
        return [TextContent(type="text", text=result_text)]
        
    except Exception as e:
        return [TextContent(
            type="text",
            text=f"❌ Hata: {str(e)}\n\nNot: sentence-transformers yüklü mü? pip install sentence-transformers"
        )]


async def handle_search_analysis_from_vector_db(arguments: Dict) -> List[TextContent]:
    """Vector database'den RAG ile analiz sonuçlarını ara"""
    tenant_slug = arguments.get("tenant_slug")
    query_text = arguments.get("query_text")
    limit = arguments.get("limit", 5)
    score_threshold = arguments.get("score_threshold", 0.5)
    filter_type = arguments.get("filter_type")
    
    # Tenant doğrulama
    mongo = get_mongo_client()
    db = mongo["airqoonBaseMapDB"]
    tenant = db["Tenants"].find_one({"SlugName": tenant_slug})
    
    if not tenant:
        return [TextContent(
            type="text",
            text=f"❌ Tenant bulunamadı: {tenant_slug}"
        )]
    
    # Vector DB'de ara
    vector_api = get_vector_api()
    try:
        # Filter hazırla (varsa)
        filter_metadata = None
        if filter_type:
            filter_metadata = {"analysis_type": filter_type}
        
        results = vector_api.search_analysis(
            tenant_slug=tenant_slug,
            query_text=query_text,
            limit=limit,
            score_threshold=score_threshold,
            filter_metadata=filter_metadata
        )
        
        result_text = f"# RAG Arama Sonuçları\n\n"
        result_text += f"**Tenant:** {tenant_slug}\n"
        result_text += f"**Sorgu:** {query_text}\n"
        result_text += f"**Bulunan Sonuç:** {len(results)} adet\n\n"
        
        if not results:
            result_text += "⚠️ Sorgunuza uygun analiz bulunamadı.\n"
            result_text += "Score threshold'u düşürmeyi deneyin veya farklı bir sorgu deneyin.\n"
        else:
            result_text += "## Sonuçlar\n\n"
            for i, result in enumerate(results, 1):
                score = result.get("score", 0)
                payload = result.get("payload", {})
                text = payload.get("text", "N/A")
                analysis_type = payload.get("analysis_type", "unknown")
                created_at = payload.get("created_at", "N/A")
                
                result_text += f"### {i}. Sonuç (Similarity: {score:.3f})\n\n"
                result_text += f"**Analiz Tipi:** {analysis_type}\n"
                result_text += f"**Oluşturulma Tarihi:** {created_at}\n"
                result_text += f"**Similarity Score:** {score:.3f}\n\n"
                result_text += f"**Analiz Metni:**\n```\n{text[:500]}{'...' if len(text) > 500 else ''}\n```\n\n"
                result_text += "---\n\n"
        
        return [TextContent(type="text", text=result_text)]
        
    except Exception as e:
        return [TextContent(
            type="text",
            text=f"❌ Hata: {str(e)}\n\nNot: sentence-transformers yüklü mü? pip install sentence-transformers"
        )]


async def main():
    """MCP server'ı başlat"""
    import sys
    import traceback
    from mcp.server.lowlevel import NotificationOptions
    
    try:
        async with stdio_server() as (read_stream, write_stream):
            # Capabilities'i oluştur
            notification_options = NotificationOptions()
            capabilities = server.get_capabilities(
                notification_options=notification_options,
                experimental_capabilities={}
            )
            
            await server.run(
                read_stream,
                write_stream,
                InitializationOptions(
                    server_name="airqoon-analyzer",
                    server_version="1.0.0",
                    capabilities=capabilities
                )
            )
    except Exception as e:
        error_msg = f"MCP Server Error: {e}\n{traceback.format_exc()}"
        print(error_msg, file=sys.stderr)
        sys.exit(1)


def run_http_server():
    app = Flask(__name__)

    @app.get("/health")
    def health():
        return jsonify({"status": "ok"})

    @app.get("/healthz")
    def healthz():
        if not _http_ready.is_set():
            return jsonify({"status": "starting"}), 503
        return jsonify({"status": "ok"})

    @app.post("/call_tool")
    def call_tool_http():
        payload = request.get_json(silent=True) or {}
        tool_name = payload.get("tool")
        arguments = payload.get("arguments") or {}

        if not tool_name:
            return jsonify({"error": "tool is required"}), 400

        try:
            result = asyncio.run(call_tool(tool_name, arguments))
            text = "\n".join([c.text for c in result if getattr(c, "type", None) == "text"]) if result else ""
            return jsonify({"text": text})
        except Exception as e:
            return jsonify({"error": str(e)}), 500

    port = int(os.getenv("MCP_HTTP_PORT", "5005"))

    # Warm up embedding model once at startup to avoid cold-start latency on first request.
    # Start it in a background thread (non-blocking). Can be disabled by setting MCP_WARMUP=0.
    if os.getenv("MCP_WARMUP", "1") == "1":
        try:
            def _warmup():
                try:
                    from embedding_utils import get_embedding_model
                    get_embedding_model()
                    _http_ready.set()
                except Exception as e:
                    print(f"⚠️ MCP warm-up failed: {e}")

            threading.Thread(target=_warmup, daemon=True).start()
        except Exception as e:
            print(f"⚠️ MCP warm-up thread start failed: {e}")
    else:
        _http_ready.set()

    app.run(host="0.0.0.0", port=port, debug=False)


if __name__ == "__main__":
    if os.getenv("MCP_HTTP", "0") == "1":
        run_http_server()
    else:
        asyncio.run(main())
