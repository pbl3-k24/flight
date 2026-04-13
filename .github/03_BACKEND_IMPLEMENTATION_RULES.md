# BACKEND IMPLEMENTATION RULES

## Services

* Business logic belongs in Application Services or Domain Methods
* Controllers must remain thin

## DTO Rules

* Never expose entities directly from API
* Use Create/Update/Response DTOs

## Validation

* Validate input before service execution
* Use guard clauses for domain invariants

## Async Rules

* All IO operations async
* Repository methods return Task / Task<T>

## Error Handling

* Throw domain/application exceptions where appropriate
* Never swallow exceptions silently
