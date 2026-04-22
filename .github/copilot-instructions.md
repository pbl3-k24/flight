These rules are mandatory and override any default AI behavior.
## Clean Code Enforcement Rules

When generating or modifying code, you MUST strictly follow these principles:

### 1. Naming Conventions

* Use meaningful, intention-revealing names.
* Avoid abbreviations unless universally understood.
* Variables, functions, and classes must clearly describe their purpose.

### 2. Function Design

* Each function must do ONE thing only.
* Functions should be small (preferably < 30 lines).
* Avoid deep nesting (max 2–3 levels).

### 3. Readability

* Code must be self-explanatory without excessive comments.
* Prefer clarity over cleverness.
* Maintain consistent formatting and structure.

### 4. Comments

* Do NOT write redundant comments.
* Only explain "why", not "what".
* Remove commented-out code.

### 5. Error Handling

* Handle exceptions explicitly.
* Do not swallow errors silently.
* Provide meaningful error messages.

### 6. DRY Principle

* Avoid code duplication.
* Extract reusable logic into functions or modules.

### 7. SOLID Principles

* Follow SOLID where applicable.
* Ensure proper separation of concerns.

### 8. Dependencies

* Minimize coupling.
* Use dependency injection where possible.

### 9. Testing Awareness

* Code should be testable.
* Avoid hard-coded values and side effects.

### 10. Refactoring Requirement

* If existing code is messy, you MUST refactor it before adding new logic.
* Do not build on top of bad code.

Failure to follow these rules means the code is NOT acceptable.
