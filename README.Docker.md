# Docker Setup Guide for Retail Execution Audit System

This guide will help you set up and run the Retail Execution Audit System using Docker containers.

## Prerequisites

- Docker Desktop or Docker Engine (version 20.10 or later)
- Docker Compose (version 2.0 or later)
- At least 4GB of available RAM
- At least 10GB of free disk space

## Quick Start

### 1. Clone and Navigate to Project
```bash
git clone <repository-url>
cd AuditSystem-.NET-
```

### 2. Run the Full Stack (Production-like)
```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f api
```

### 3. Run Development Environment (Services Only)
```bash
# Start only the infrastructure services (PostgreSQL, Redis, RabbitMQ)
docker-compose -f docker-compose.dev.yml up -d

# Run the API locally with dotnet run
cd src/AuditSystem.API
dotnet run
```

## Services and Ports

| Service | Port | Purpose | Access |
|---------|------|---------|---------|
| **API** | 8080 | Main .NET API | http://localhost:8080 |
| **PostgreSQL** | 5432 | Database | localhost:5432 |
| **Redis** | 6379 | Cache | localhost:6379 |
| **RabbitMQ** | 5672 | Message Queue | localhost:5672 |
| **RabbitMQ Management** | 15672 | Web UI | http://localhost:15672 |
| **pgAdmin** | 5050 | PostgreSQL Admin | http://localhost:5050 |

## Service Credentials

### Production Environment (docker-compose.yml)
- **PostgreSQL**: `postgres` / `audit_password_123`
- **Redis**: Password: `redis_password_123`
- **RabbitMQ**: `audit_user` / `rabbitmq_password_123`
- **pgAdmin**: `admin@audit-system.com` / `pgadmin_password_123`

### Development Environment (docker-compose.dev.yml)
- **PostgreSQL**: `postgres` / `123456`
- **Redis**: No password
- **RabbitMQ**: `guest` / `guest`
- **pgAdmin**: `admin@audit-system.com` / `admin123`

## API Endpoints

- **Health Check**: http://localhost:8080/health
- **Swagger UI**: http://localhost:8080/swagger (in development)
- **API Base**: http://localhost:8080/api/v1/
- **pgAdmin**: http://localhost:5050 (PostgreSQL database management)

## Docker Commands Reference

### Build and Start
```bash
# Build and start all services
docker-compose up -d

# Build with no cache
docker-compose build --no-cache

# Start specific service
docker-compose up postgres redis rabbitmq
```

### Logs and Monitoring
```bash
# View all logs
docker-compose logs

# Follow logs for specific service
docker-compose logs -f api

# View last 100 lines
docker-compose logs --tail=100 api
```

### Database Management
```bash
# Connect to PostgreSQL
docker exec -it audit_postgres psql -U postgres -d retail-execution-audit-system

# View database logs
docker-compose logs postgres

# Backup database
docker exec audit_postgres pg_dump -U postgres retail-execution-audit-system > backup.sql
```

### Redis Management
```bash
# Connect to Redis CLI
docker exec -it audit_redis redis-cli

# Monitor Redis commands
docker exec -it audit_redis redis-cli MONITOR
```

### RabbitMQ Management
```bash
# Access RabbitMQ Management UI
# Navigate to: http://localhost:15672
# Login with credentials from the service section above

# View RabbitMQ logs
docker-compose logs rabbitmq
```

### pgAdmin Management
```bash
# Access pgAdmin Web Interface
# Navigate to: http://localhost:5050
# Login with credentials from the service section above

# View pgAdmin logs
docker-compose logs pgadmin

# Reset pgAdmin data (if needed)
docker-compose down
docker volume rm auditsystem-net-_pgadmin_data
docker-compose up -d pgadmin
```

### Cleanup
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (⚠️ This will delete all data)
docker-compose down -v

# Remove images
docker-compose down --rmi all
```

## Development Workflow

### Option 1: Full Docker Development
```bash
# Start all services including API
docker-compose up -d

# Make code changes and rebuild API
docker-compose build api
docker-compose up -d api
```

### Option 2: Hybrid Development (Recommended)
```bash
# Start infrastructure services only
docker-compose -f docker-compose.dev.yml up -d

# Run API locally for faster development
cd src/AuditSystem.API
dotnet watch run
```

## Database Initialization

The database is automatically initialized with the schema from `init-scripts/01-init-database.sql` when the PostgreSQL container starts for the first time.

To reset the database:
```bash
# Stop and remove the database volume
docker-compose down
docker volume rm auditsystem-net-_postgres_data

# Start again (will recreate and initialize)
docker-compose up -d postgres
```

## Using pgAdmin

pgAdmin provides a web-based interface for managing your PostgreSQL database. It's automatically configured to connect to your PostgreSQL container.

### Accessing pgAdmin

1. **Start the services**: `docker-compose up -d` or `docker-compose -f docker-compose.dev.yml up -d`
2. **Open pgAdmin**: Navigate to http://localhost:5050
3. **Login**: Use the credentials from the Service Credentials section above
4. **Database servers are pre-configured**: You'll see "Audit System Database" and "Audit System - retail-execution-audit-system" servers ready to use

### pgAdmin Features

- **Query Tool**: Execute SQL queries directly
- **Visual Table Editor**: Create and modify tables with a GUI
- **Data Viewer**: Browse and edit table data
- **Database Backup/Restore**: Export and import database dumps
- **Performance Monitoring**: View query performance and database statistics
- **User Management**: Manage PostgreSQL users and permissions

### Common pgAdmin Tasks

```sql
-- View all tables
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';

-- Check sample data
SELECT * FROM users LIMIT 5;
SELECT * FROM templates LIMIT 5;
SELECT * FROM audits LIMIT 5;

-- View database size
SELECT pg_size_pretty(pg_database_size('retail-execution-audit-system'));
```

### Troubleshooting pgAdmin

- **Can't connect to server**: Make sure PostgreSQL container is running and healthy
- **Authentication failed**: Check the pgpass file matches your PostgreSQL password
- **Server not appearing**: Clear browser cache and refresh pgAdmin
- **Permission denied**: Ensure pgAdmin configuration files have correct permissions

## Environment Variables

### Key Configuration Variables
```bash
# Database
POSTGRES_DB=retail-execution-audit-system
POSTGRES_USER=postgres
POSTGRES_PASSWORD=audit_password_123

# JWT
JWT_SECRET=YourProductionSecretKeyHereMakeSureItIsAtLeast32CharactersLongAndSecure
JWT_ISSUER=AuditSystem
JWT_AUDIENCE=AuditSystemClients

# Redis
REDIS_PASSWORD=redis_password_123

# RabbitMQ
RABBITMQ_USER=audit_user
RABBITMQ_PASSWORD=rabbitmq_password_123
RABBITMQ_VHOST=audit_vhost
```

## Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # If ports are already in use, modify the port mappings in docker-compose.yml
   # For example, change "8080:8080" to "8081:8080"
   ```

2. **Database Connection Issues**
   ```bash
   # Check if PostgreSQL is ready
   docker-compose logs postgres
   
   # Test connection
   docker exec audit_postgres pg_isready -U postgres
   ```

3. **API Won't Start**
   ```bash
   # Check API logs
   docker-compose logs api
   
   # Rebuild API container
   docker-compose build --no-cache api
   docker-compose up -d api
   ```

4. **Volume Permission Issues (Linux/Mac)**
   ```bash
   # Fix permissions for log volumes
   sudo chown -R $USER:$USER ./logs
   ```

### Performance Optimization

1. **Allocate More Memory to Docker**
   - Increase Docker Desktop memory limit to at least 4GB

2. **Use Docker Volume for Better Performance**
   - The compose files already use named volumes for optimal performance

3. **Enable BuildKit for Faster Builds**
   ```bash
   export DOCKER_BUILDKIT=1
   docker-compose build
   ```

## Monitoring and Health Checks

All services include health checks that you can monitor:

```bash
# Check service health
docker-compose ps

# View health check logs
docker inspect audit_api --format='{{.State.Health}}'
```

## Production Deployment

For production deployment:

1. Update passwords and secrets in environment variables
2. Configure proper SSL/TLS termination
3. Set up log aggregation
4. Configure backup strategies
5. Implement monitoring and alerting

## Support

If you encounter issues:

1. Check the logs: `docker-compose logs [service-name]`
2. Verify all services are healthy: `docker-compose ps`
3. Review the troubleshooting section above
4. Check Docker resource allocation (CPU, Memory, Disk) 