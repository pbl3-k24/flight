# 🗄️ Database & PostgreSQL Prompts

Reference guides:
- .github/instructions/devops/postgresql-docker-guide.md
- docs/implementation/infrastructure/DBCONTEXT_IMPLEMENTATION_GUIDE.md
- .github/instructions/backend/infrastructure-layer-guide.md

---

## Prompt 1: Update Program.cs for PostgreSQL

Based on .github/instructions/devops/postgresql-docker-guide.md:

Update backend/API/Program.cs:
- DbContext: UseNpgsql
- Redis configuration
- DI setup
- Auto migrations on startup

---

## Prompt 2: Create/Update FlightBookingDbContext

Based on docs/implementation/infrastructure/DBCONTEXT_IMPLEMENTATION_GUIDE.md:

Create/Update backend/API/Infrastructure/Data/FlightBookingDbContext.cs:
- DbSet properties for all entities
- OnConfiguring with Npgsql options
- OnModelCreating to apply configurations
- SaveChangesAsync override

---

## Prompt 3: Create Entity Configuration

Based on .github/instructions/backend/infrastructure-layer-guide.md:

Create backend/API/Infrastructure/Configurations/FlightConfiguration.cs:
- Properties configuration
- Relationships
- Indexes
- PostgreSQL-specific