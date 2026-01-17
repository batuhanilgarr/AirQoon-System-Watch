-- Postgres init for AirQoon
-- Creates required DB(s), schema and minimal seed data (idempotent)

\connect postgres

-- Create chat/admin database used by EF Core if it doesn't exist
SELECT format('CREATE DATABASE %I', 'airqoon_chat')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'airqoon_chat')
\gexec

\connect airqoon

CREATE TABLE IF NOT EXISTS public.air_quality_index (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(255) NOT NULL,
    parameter VARCHAR(50) NOT NULL,
    concentration DECIMAL(10, 2),
    concentration_unit VARCHAR(20),
    calculated_datetime TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_device_id ON public.air_quality_index (device_id);
CREATE INDEX IF NOT EXISTS idx_parameter ON public.air_quality_index (parameter);
CREATE INDEX IF NOT EXISTS idx_calculated_datetime ON public.air_quality_index (calculated_datetime);
CREATE INDEX IF NOT EXISTS idx_device_datetime ON public.air_quality_index (device_id, calculated_datetime);

INSERT INTO public.air_quality_index (device_id, parameter, concentration, concentration_unit, calculated_datetime)
SELECT 'demo-device-1', 'PM2.5-24h', 12.34, 'µg/m³', NOW() - INTERVAL '1 hour'
WHERE NOT EXISTS (
    SELECT 1 FROM public.air_quality_index
    WHERE device_id = 'demo-device-1'
      AND parameter = 'PM2.5-24h'
);
