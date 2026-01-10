# Docker Configuration for Qalam Project

This document describes how to run the Qalam project using Docker Compose.

## Architecture

The Docker setup includes:
- **Qalam API** (Port 8080) - Main backend API
- **MessagingApi** (Port 5001) - Email/SMS/Push notification microservice
- **RabbitMQ** (Ports 5672, 15672) - Message broker for async processing
- **Remote Database** - SQL Server on `db37349.public.databaseasp.net`

## Prerequisites

- Docker Desktop installed and running
- Access to the remote database server
- MessagingApi project available at `../Train-Backend/apps/backend/Sudan_Train.MessagingApi`

## Quick Start

1. **Navigate to the Qalam project directory:**
   ```bash
   cd "C:\Users\user\OneDrive\المستندات\Visual Studio 2022\projects\Qalam"
   ```

2. **Apply database migrations (REQUIRED before first run):**
   ```bash
   dotnet ef database update --project Qalam.Infrastructure --startup-project Qalam.Api
   ```
   
   This command will:
   - Create the database if it doesn't exist
   - Apply all pending migrations to bring the schema up to date

3. **Start all services:**
   ```bash
   docker-compose up -d
   ```

4. **View logs:**
   ```bash
   docker-compose logs -f
   ```

5. **Stop all services:**
   ```bash
   docker-compose down
   ```

## Services

### Qalam API
- **URL**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **Logs**: Mounted at `./Qalam.Api/Logs`

### MessagingApi
- **URL**: http://localhost:5001
- **Swagger UI**: http://localhost:5001/swagger
- **Purpose**: Handles email, SMS, and push notifications

### RabbitMQ
- **AMQP Port**: 5672
- **Management UI**: http://localhost:15672
- **Credentials**: guest / guest
- **Purpose**: Message queue for async email processing

## Environment Variables

You can customize the configuration using environment variables or a `.env` file:

### Database Connection
```bash
ConnectionStrings__dbcontext=Server=your-server;Database=your-db;...
```

### JWT Settings
```bash
JWT_SECRET=your-secret-key
JWT_ISSUER=QalamProject
JWT_AUDIENCE=QalamProjectUsers
```

### MessagingApi URL
In Docker, this is automatically set to `http://messaging-api` (internal Docker network).

### Email Settings
```bash
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_FROM_EMAIL=noreply@qalam.com
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-password
```

### Other Settings
See `docker-compose.yml` for all available environment variables.

## Building Images

To rebuild the images:

```bash
docker-compose build
```

To rebuild without cache:

```bash
docker-compose build --no-cache
```

## Database Migration Management

### Running Migrations

Migrations are now managed manually using EF Core CLI tools. This provides better control over database schema changes, especially in production environments.

#### Apply All Pending Migrations
```bash
dotnet ef database update --project Qalam.Infrastructure --startup-project Qalam.Api
```

#### Create a New Migration
```bash
dotnet ef migrations add <MigrationName> --project Qalam.Infrastructure --startup-project Qalam.Api
```

#### Rollback to a Specific Migration
```bash
dotnet ef database update <MigrationName> --project Qalam.Infrastructure --startup-project Qalam.Api
```

#### List All Migrations
```bash
dotnet ef migrations list --project Qalam.Infrastructure --startup-project Qalam.Api
```

#### Remove Last Migration (if not applied)
```bash
dotnet ef migrations remove --project Qalam.Infrastructure --startup-project Qalam.Api
```

### Database Seeding

Database seeders (roles, users, etc.) are available in the codebase but are not automatically executed. To seed data:

1. The seeder classes are located in `Qalam.Infrastructure/Seeder/`
2. You can create a separate admin endpoint or console tool to trigger seeding
3. Or manually insert seed data using SQL scripts

### Important Notes

- **Always run migrations before starting the application** for the first time or after pulling new migration files
- Migrations are **not applied automatically** at application startup
- This prevents unexpected schema changes in production environments
- For Docker deployments, run migrations on the host machine before starting containers, or add a migration step to your CI/CD pipeline

## Troubleshooting

### Database Connection Issues
- Verify the remote database server is accessible
- Check firewall rules allow connections from your Docker host
- Ensure the connection string in `docker-compose.yml` is correct

### MessagingApi Build Fails
- Verify the path `../Train-Backend/apps/backend` exists relative to the Qalam project
- Ensure the MessagingApi project builds successfully locally

### Port Conflicts
If ports 8080, 5001, 5672, or 15672 are already in use:
- Stop the conflicting services
- Or modify the port mappings in `docker-compose.yml`

### View Service Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f qalam-api
docker-compose logs -f messaging-api
docker-compose logs -f rabbitmq
```

### Restart a Service
```bash
docker-compose restart qalam-api
```

### Remove Everything (including volumes)
```bash
docker-compose down -v
```

## Network Communication

All services communicate via the `qalam-network` Docker network:
- Qalam API → MessagingApi: `http://messaging-api`
- MessagingApi → RabbitMQ: `rabbitmq:5672`
- Both APIs → Remote Database: External connection

## Data Persistence

- **RabbitMQ**: Data persisted in `rabbitmq_data` volume
- **Logs**: Qalam API logs are mounted from `./Qalam.Api/Logs`
- **Database**: Remote server (no local persistence)

## Production Considerations

1. **Security**:
   - Change default RabbitMQ credentials
   - Use strong JWT secrets
   - Secure database connection strings
   - Use environment variables or secrets management

2. **Performance**:
   - Adjust resource limits in `docker-compose.yml`
   - Configure connection pooling
   - Monitor service health

3. **Monitoring**:
   - Set up health checks
   - Configure logging aggregation
   - Monitor RabbitMQ queues

4. **Backup**:
   - Backup remote database regularly
   - Export RabbitMQ configuration if needed

