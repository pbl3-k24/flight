# ARCHITECTURE RULES

## Layer Boundaries

Domain:

* Entities
* Value Objects
* Domain Logic

Application:

* DTOs
* Interfaces
* Use Cases / Services

Infrastructure:

* EF Core
* Repository Implementations
* External Integrations

Presentation:

* API Controllers / Frontend

---

## Dependency Rules

Allowed:
Presentation → Application
Application → Domain
Infrastructure → Application / Domain

Forbidden:
Domain → Infrastructure
Application → Presentation
Infrastructure → Presentation

---

## File Placement Rules

Entities:
Domain/Entities

Configurations:
Infrastructure/Configurations

Repositories:
Infrastructure/Repositories

Interfaces:
Application/Interfaces

Services:
Application/Services
