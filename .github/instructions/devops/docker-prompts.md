# 🐳 Docker & Docker Compose Prompts

Reference guides:
- .github/instructions/devops/docker-setup.md
- .github/instructions/devops/postgresql-docker-guide.md
- docs/architecture/architecture-overview.md

---

## Prompt 1: Create docker-compose.yml

Based on .github/instructions/devops/docker-setup.md:

I have docker/ folder at project root.
Create docker-compose.yml with:
- PostgreSQL 16 service
- Redis 7 service
- ASP.NET Core API service
- All connected to flight-booking-network
- Health checks for all services
- Persistent volumes

Reference: .github/instructions/devops/postgresql-docker-guide.md

---

## Prompt 2: Create Dockerfile

Based on .github/instructions/devops/docker-setup.md:

Create multi-stage Dockerfile:
- Build stage: SDK 8.0
- Runtime stage: aspnet 8.0
- Copy backend/API.csproj
- Health check: curl /health
- Entry point: dotnet FlightTicketBooking.API.dll

---

## Prompt 3: Create init-db.sql

Based on .github/instructions/devops/postgresql-docker-guide.md:

Create database/init-db.sql for PostgreSQL initialization