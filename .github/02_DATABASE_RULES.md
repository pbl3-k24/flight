# DATABASE RULES

## Entity Rules

* Every entity must have PK
* Use int PK unless otherwise specified
* Required fields must be non-nullable
* Optional FK must be nullable

## Constraint Rules

* Add unique indexes where business requires uniqueness
* Add composite keys for junction tables
* Add check constraints for critical invariants

## Naming

Tables: PascalCase pluralized
PK: Id
FK: <EntityName>Id

## Migration Rules

* One logical migration per completed feature set
* Never generate migrations with compile errors present

## Inventory Rules

Seat inventory must enforce:

* AvailableSeats >= 0
* HeldSeats >= 0
* SoldSeats >= 0
* Available + Held + Sold <= TotalSeats

