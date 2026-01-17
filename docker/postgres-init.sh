#!/bin/sh
set -e

# Create chat/admin database used by EF Core
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'airqoon_chat') THEN
            CREATE DATABASE airqoon_chat;
        END IF;
    END $$;
EOSQL

# Create air_quality_index table in the main database (airqoon)
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE TABLE IF NOT EXISTS air_quality_index (
        id SERIAL PRIMARY KEY,
        device_id VARCHAR(255) NOT NULL,
        parameter VARCHAR(50) NOT NULL,
        concentration DECIMAL(10, 2),
        concentration_unit VARCHAR(20),
        calculated_datetime TIMESTAMP NOT NULL,
        created_at TIMESTAMP DEFAULT NOW()
    );

    CREATE INDEX IF NOT EXISTS idx_device_id ON air_quality_index (device_id);
    CREATE INDEX IF NOT EXISTS idx_parameter ON air_quality_index (parameter);
    CREATE INDEX IF NOT EXISTS idx_calculated_datetime ON air_quality_index (calculated_datetime);
    CREATE INDEX IF NOT EXISTS idx_device_datetime ON air_quality_index (device_id, calculated_datetime);

    -- Seed minimal sample data (idempotent)
    -- This allows the system to work out-of-the-box if you don't restore a real dump.
    INSERT INTO air_quality_index (device_id, parameter, concentration, concentration_unit, calculated_datetime)
    SELECT 'demo-device-1', 'PM2.5-24h', 12.34, 'µg/m³', NOW() - INTERVAL '1 hour'
    WHERE NOT EXISTS (
        SELECT 1 FROM air_quality_index
        WHERE device_id = 'demo-device-1'
          AND parameter = 'PM2.5-24h'
    );
EOSQL
