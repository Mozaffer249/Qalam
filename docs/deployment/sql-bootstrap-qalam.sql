-- Qalam SQL bootstrap: four databases + staging/prod logins (password: Qalam@2026)
-- Run as SA from the VPS (see docs/deployment/01-sql-server-install.md):
--   sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C -i docs/deployment/sql-bootstrap-qalam.sql

-- Staging databases
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_staging')
    CREATE DATABASE qalam_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_messaging_staging')
    CREATE DATABASE qalam_messaging_staging;
GO

-- Production databases
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_prod')
    CREATE DATABASE qalam_prod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_messaging_prod')
    CREATE DATABASE qalam_messaging_prod;
GO

-- Staging login
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'qalam_staging_user')
    CREATE LOGIN qalam_staging_user WITH PASSWORD = 'Qalam@2026';
ELSE
    ALTER LOGIN qalam_staging_user WITH PASSWORD = 'Qalam@2026';
GO

USE qalam_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_staging_user')
BEGIN
    CREATE USER qalam_staging_user FOR LOGIN qalam_staging_user;
    ALTER ROLE db_owner ADD MEMBER qalam_staging_user;
END
GO

USE qalam_messaging_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_staging_user')
BEGIN
    CREATE USER qalam_staging_user FOR LOGIN qalam_staging_user;
    ALTER ROLE db_owner ADD MEMBER qalam_staging_user;
END
GO

-- Production login
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'qalam_prod_user')
    CREATE LOGIN qalam_prod_user WITH PASSWORD = 'Qalam@2026';
ELSE
    ALTER LOGIN qalam_prod_user WITH PASSWORD = 'Qalam@2026';
GO

USE qalam_prod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_prod_user')
BEGIN
    CREATE USER qalam_prod_user FOR LOGIN qalam_prod_user;
    ALTER ROLE db_owner ADD MEMBER qalam_prod_user;
END
GO

USE qalam_messaging_prod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_prod_user')
BEGIN
    CREATE USER qalam_prod_user FOR LOGIN qalam_prod_user;
    ALTER ROLE db_owner ADD MEMBER qalam_prod_user;
END
GO
