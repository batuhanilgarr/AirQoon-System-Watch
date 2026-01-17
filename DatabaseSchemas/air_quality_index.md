# air_quality_index (PostgreSQL)

```sql
CREATE TABLE air_quality_index (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(255) NOT NULL,
    parameter VARCHAR(50) NOT NULL,
    concentration DECIMAL(10, 2),
    concentration_unit VARCHAR(20),
    calculated_datetime TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_device_id ON air_quality_index (device_id);
CREATE INDEX idx_parameter ON air_quality_index (parameter);
CREATE INDEX idx_calculated_datetime ON air_quality_index (calculated_datetime);
CREATE INDEX idx_device_datetime ON air_quality_index (device_id, calculated_datetime);
```

## Parametre Normalizasyonu

- PM10 -> PM10-24h
- PM2.5 veya PM25 -> PM2.5-24h
- NO2 -> NO2-1h
- O3 -> O3-1h
- SO2 -> SO2-1h
- CO -> CO-8h
