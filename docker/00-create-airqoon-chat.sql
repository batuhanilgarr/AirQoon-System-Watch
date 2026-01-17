-- Create chat/admin database used by EF Core if it doesn't exist
\connect postgres

-- Ensure roles referenced by the dump exist
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'airqoon-db') THEN
    CREATE ROLE "airqoon-db" LOGIN;
  END IF;
END $$;

SELECT format('CREATE DATABASE %I', 'airqoon_chat')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'airqoon_chat')
\gexec
