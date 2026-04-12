# 🚀 Quick Start Guide - PostgreSQL + Docker Setup

## 📋 Prerequisites
- Docker Desktop installed
- Docker Compose version 3.8+
- .NET 8 SDK (for local development)
- Git

---

## ⚡ 5-Minute Setup

### Step 1: Tạo docker-compose.yml
Copy file từ output vào root của project:
```bash
# Copy docker-compose.yml vào: {project-root}/docker-compose.yml
```

### Step 2: Tạo Dockerfile
Copy Dockerfile từ output vào root:
```bash
# Copy Dockerfile vào: {project-root}/Dockerfile
```

### Step 3: Update appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Step 4: Start Services
```bash
# From project root
docker-compose up -d

# Check status (wait for healthy)
docker-compose ps

# View logs
docker-compose logs -f
```

### Step 5: Apply Migrations
```bash
# Inside the API container
docker-compose exec api dotnet ef database update
```

### Step 6: Test API
```bash
# Swagger UI
curl http://localhost:5000/swagger

# Or open browser: http://localhost:5000/swagger/index.html
```

---

## 📊 Verify Everything Works

### Check Services Status
```bash
docker-compose ps

# Output should show:
# postgres        Up (healthy)
# redis           Up (healthy)  
# api             Up (healthy)
```

### Test Database Connection
```bash
docker-compose exec postgres psql -U postgres -d FlightTicketBooking -c "\dt"
```

### Test Redis Connection
```bash
docker-compose exec redis redis-cli ping
# Should output: PONG
```

### Test API Health
```bash
curl http://localhost:5000/health

# Or check logs:
docker-compose logs api
```

---

## 📝 Useful Commands

### View Logs
```bash
docker-compose logs -f api          # API logs
docker-compose logs -f postgres     # Database logs
docker-compose logs -f redis        # Cache logs
```

### Database Operations
```bash
# Access PostgreSQL CLI
docker-compose exec postgres psql -U postgres -d FlightTicketBooking

# Backup database
docker-compose exec postgres pg_dump -U postgres FlightTicketBooking > backup.sql

# View tables
docker-compose exec postgres psql -U postgres -d FlightTicketBooking -c "\dt"
```

### Restart Services
```bash
docker-compose restart api
docker-compose restart postgres
docker-compose restart redis
```

### Stop Everything
```bash
docker-compose down
```

### Clean Up (WARNING: Removes data!)
```bash
docker-compose down -v  # -v removes volumes
```

---

## 🔧 Connection Strings

### Local Development (no Docker)
```
Host=localhost;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres
```

### Docker (from Host Machine)
```
Host=localhost;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres
```

### Docker (from Inside Container/API)
```
Host=postgres;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres
```

### PostgreSQL CLI (from Docker)
```bash
docker-compose exec postgres psql -U postgres -d FlightTicketBooking
```

---

## 🐛 Troubleshooting

### Issue: "Cannot connect to postgres"
```bash
# Solution 1: Check if service is running
docker-compose ps

# Solution 2: Check logs
docker-compose logs postgres

# Solution 3: Wait for health check
docker-compose ps  # Wait for (healthy)

# Solution 4: Restart
docker-compose restart postgres
```

### Issue: "Database does not exist"
```bash
# Solution: Migrations not applied
docker-compose exec api dotnet ef database update

# Or create manually:
docker-compose exec postgres psql -U postgres -c "CREATE DATABASE FlightTicketBooking;"
```

### Issue: "Port 5432 already in use"
```bash
# Solution: Change port in docker-compose.yml
ports:
  - "5433:5432"  # Use 5433 instead

# Or stop existing PostgreSQL:
docker stop <container-name>
```

### Issue: API can't connect to postgres
```bash
# Check if postgres is healthy:
docker-compose ps

# Verify hostname is 'postgres' (not 'localhost') in API connection string
Host=postgres;Port=5432;...
```

### Issue: Migrations fail
```bash
# Check migration file syntax
dotnet ef migrations list

# Remove last migration if bad:
dotnet ef migrations remove

# Recreate:
dotnet ef migrations add FixedMigration
dotnet ef database update
```

---

## 📦 Environment Variables (Optional)

Create `.env` file:
```
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=FlightTicketBooking
ASPNETCORE_ENVIRONMENT=Production
```

Reference in docker-compose.yml:
```yaml
environment:
  POSTGRES_USER: ${POSTGRES_USER}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

---

## 📋 Folder Structure After Setup

```
project-root/
├── docker-compose.yml           ← From outputs
├── Dockerfile                   ← From outputs
├── FlightTicketBooking.sln
├── .github/
│   └── copilot-instructions.md  ← From outputs
├── FlightTicketBooking.API/
│   ├── Controllers/
│   ├── DTOs/
│   ├── appsettings.json
│   ├── appsettings.Docker.json
│   └── Program.cs
├── FlightTicketBooking.Application/
├── FlightTicketBooking.Domain/
├── FlightTicketBooking.Infrastructure/
├── FlightTicketBooking.Tests/
└── backend/
    ├── 01_API_LAYER_GUIDE.md
    ├── 02_APPLICATION_LAYER_GUIDE.md
    ├── 03_DOMAIN_LAYER_GUIDE.md
    ├── 04_INFRASTRUCTURE_LAYER_GUIDE.md
    ├── 05_TESTING_GUIDE.md
    ├── 06_POSTGRESQL_DOCKER_GUIDE.md
    └── COPILOT.md
```

---

## ✅ Setup Checklist

- [ ] Docker Desktop installed and running
- [ ] docker-compose.yml in project root
- [ ] Dockerfile in project root
- [ ] appsettings.json has correct connection string
- [ ] `docker-compose up -d` succeeds
- [ ] `docker-compose ps` shows all services (healthy)
- [ ] `docker-compose exec api dotnet ef database update` succeeds
- [ ] `curl http://localhost:5000/swagger` returns 200
- [ ] Logs show no errors: `docker-compose logs`
- [ ] Database accessible: `docker-compose exec postgres psql ...`

---

## 🚀 Next Steps

1. **Generate Code with Copilot**
   - Use prompts from `.github/copilot-instructions.md`
   - Reference guide files: `06_POSTGRESQL_DOCKER_GUIDE.md`

2. **Create Controllers**
   - Use Prompt 1 (API Layer)
   - Reference: 01_API_LAYER_GUIDE.md

3. **Create Services**
   - Use Prompt 5 (Application Layer)
   - Reference: 02_APPLICATION_LAYER_GUIDE.md

4. **Create Entities**
   - Use Prompt 7 (Domain Layer)
   - Reference: 03_DOMAIN_LAYER_GUIDE.md

5. **Create Repositories**
   - Use Prompt 11 (Infrastructure Layer)
   - Reference: 04_INFRASTRUCTURE_LAYER_GUIDE.md

6. **Write Tests**
   - Use Prompt 15 (Testing)
   - Reference: 05_TESTING_GUIDE.md

---

## 📞 Common Commands Reference

| Command | Purpose |
|---------|---------|
| `docker-compose up -d` | Start all services |
| `docker-compose ps` | Check service status |
| `docker-compose logs -f` | View live logs |
| `docker-compose down` | Stop all services |
| `docker-compose exec api bash` | SSH into API |
| `docker-compose exec postgres psql -U postgres` | Access PostgreSQL CLI |
| `docker system prune` | Clean up Docker resources |
| `docker-compose build` | Rebuild images |

---

**Quick Start Version**: 1.0  
**Last Updated**: April 2026  
**Ready for**: PostgreSQL 16 + Docker Compose + ASP.NET Core 8.0
