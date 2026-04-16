# Flight Booking API - Docker Setup Guide

## Files Created

### 1. **Dockerfile** (API/Dockerfile)
Multi-stage build for .NET 10 application with:
- ✅ SDK stage for building
- ✅ Publish stage for optimization
- ✅ Runtime stage with minimal footprint
- ✅ Health check endpoint
- ✅ Port 5000 exposure
- ✅ Production environment configuration

### 2. **.dockerignore** (API/.dockerignore)
Optimizes Docker build context by excluding:
- Git files and directories
- Build artifacts (bin, obj)
- Node modules
- IDE configurations
- Secrets and environment files

### 3. **docker-compose.yml** (root directory)
Orchestrates the complete stack:
- 🗄️ **PostgreSQL 17-alpine**: Database service
- 🎨 **pgAdmin**: Database management UI (port 5050)
- 🚀 **Flight API**: ASP.NET Core application (port 5000)
- 📦 **Redis**: Caching layer (port 6379)
- 🌐 **Network**: Internal communication between containers

## Quick Start

### Option 1: Using Docker Compose (Recommended)

```bash
# Build and start all services
docker-compose up --build

# View logs
docker-compose logs -f api

# Stop services
docker-compose down

# Stop and remove volumes (cleanup)
docker-compose down -v
```

### Option 2: Build Docker Image Only

```bash
# Navigate to project root
cd E:\pbl3\flight\backend

# Build the Docker image
docker build -f API/Dockerfile -t flight-api:latest .

# Run the container (requires external PostgreSQL)
docker run -p 5000:5000 \
  -e "ConnectionStrings__DefaultConnection=Host=your-postgres-host;Port=5432;Database=FlightBookingDB;Username=admin;Password=SecretPassword123!" \
  -e "Jwt__Key=your-super-secret-jwt-key-that-must-be-at-least-32-characters-long-for-security" \
  flight-api:latest
```

## Environment Variables

The application respects these environment variables (from appsettings.json):

### Database
```
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=FlightBookingDB;Username=admin;Password=SecretPassword123!
```

### JWT Configuration
```
Jwt__Key=your-super-secret-jwt-key-that-must-be-at-least-32-characters-long-for-security
Jwt__Issuer=flight-booking-api
Jwt__Audience=flight-booking-app
Jwt__ExpirationHours=24
```

### Email (SMTP)
```
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=your-email@gmail.com
Smtp__Password=your-app-password
Smtp__FromEmail=noreply@flightbooking.com
```

### Redis Cache
```
Redis__ConnectionString=redis:6379
```

### ASP.NET Core
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

## Accessing Services

After running `docker-compose up`:

| Service | URL | Credentials |
|---------|-----|-------------|
| **Flight API** | http://localhost:5000 | - |
| **API Swagger** | http://localhost:5000/swagger | - |
| **pgAdmin** | http://localhost:5050 | admin@example.com / admin |
| **PostgreSQL** | localhost:5432 | admin / SecretPassword123! |
| **Redis** | localhost:6379 | - |

## Health Check

The API includes a health check endpoint at `/health`:

```bash
curl http://localhost:5000/health
```

Docker will automatically restart the container if health checks fail.

## Common Commands

```bash
# View running containers
docker-compose ps

# View container logs
docker-compose logs -f api

# Execute command in container
docker-compose exec api dotnet ef database update

# Rebuild and restart
docker-compose down
docker-compose up --build

# Remove everything including volumes
docker-compose down -v
```

## Troubleshooting

### Container exits immediately
```bash
docker-compose logs api
```
Check for database connection errors or missing environment variables.

### Database connection refused
Ensure PostgreSQL container is healthy:
```bash
docker-compose ps
```
Check the `postgres` service status shows "(healthy)".

### Port already in use
Change port mappings in docker-compose.yml:
```yaml
ports:
  - "5001:5000"  # Maps container 5000 to host 5001
```

### Rebuild without cache
```bash
docker-compose build --no-cache
```

## Production Considerations

1. **Security**: Update default passwords in environment variables
2. **Secrets**: Use Docker secrets or environment variable files instead of hardcoding
3. **Logging**: Configure centralized logging (ELK, Splunk, etc.)
4. **Monitoring**: Add Prometheus/Grafana for metrics
5. **Registry**: Push image to Docker Hub or private registry
6. **SSL/TLS**: Configure HTTPS with proper certificates
7. **Database Backups**: Set up automated PostgreSQL backups
8. **Resource Limits**: Add resource constraints in docker-compose.yml

## Next Steps

1. Run `docker-compose up --build`
2. Access API at http://localhost:5000
3. Test login endpoint:
   ```bash
   curl -X POST http://localhost:5000/api/v1/users/login \
     -H "Content-Type: application/json" \
     -d '{"email":"user1@gmail.com","password":"Test@1234"}'
   ```
4. Access pgAdmin at http://localhost:5050 to view database
5. Use Swagger at http://localhost:5000/swagger to explore API

## Support

For issues or questions about the Docker setup, check:
- Application logs: `docker-compose logs api`
- Database logs: `docker-compose logs postgres`
- Docker documentation: https://docs.docker.com/
