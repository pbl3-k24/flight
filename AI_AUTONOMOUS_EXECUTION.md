# AI AUTONOMOUS EXECUTION PROTOCOL

## Objective

Regenerate the entire database layer for Flight Booking System autonomously using repository-aware code generation.

AI MUST execute tasks sequentially without asking for confirmation unless blocked by ambiguity.

---

## GLOBAL RULES

1. Read all existing code before modifying.
2. Preserve architecture consistency.
3. Do NOT generate all files at once.
4. Execute phase-by-phase.
5. Build project after each phase.
6. Fix compile/runtime/schema issues before continuing.
7. Never skip validation.
8. Stop only if architectural conflict cannot be resolved automatically.

---

## PHASE 1 — DOMAIN ENTITIES

Source of truth:

* `COPILOT_READY_PROMPTS.md`

Tasks:

1. Generate all domain entities from prompts 1–23.
2. Place in `Domain/Entities`.
3. Reuse shared abstractions/base entities if existing.
4. Ensure nullable correctness.
5. Ensure navigation properties compile.

Validation:

* Run build.
* Fix all errors before Phase 2.

---

## PHASE 2 — ENTITY CONFIGURATIONS

Tasks:

1. Generate all IEntityTypeConfiguration classes.
2. Place in `Infrastructure/Configurations`.
3. Add PK/FK/Indexes/Constraints.
4. Prevent duplicate relationship config.

Validation:

* Run build.
* Fix all EF config errors.

---

## PHASE 3 — DBCONTEXT

Tasks:

1. Update DbContext.
2. Add DbSets.
3. Apply configurations automatically.
4. Add SaveChanges override logic.

Validation:

* Build project.

---

## PHASE 4 — REPOSITORIES

Tasks:

1. Generate repository interfaces.
2. Respect architecture.

Validation:

* Build project.

---

## PHASE 5 — MIGRATION

Tasks:

1. Create Initial Migration.
2. Update Database.
3. Validate schema.

---

## FINAL VALIDATION

AI MUST ensure:

* Build succeeds
* Migration succeeds
* No duplicate FK
* No nullable mismatch
* No broken navigation
* No schema conflicts

---

## OUTPUT FORMAT

After each phase provide:

* Files created
* Files modified
* Errors fixed
* Validation result
