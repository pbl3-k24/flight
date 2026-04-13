# ERROR RECOVERY PROTOCOL

## Compile Errors

1. Read exact compiler error
2. Fix root cause, not symptom
3. Rebuild immediately

## Migration Errors

1. Inspect invalid relationship/config
2. Fix model/config mismatch
3. Regenerate migration

## Runtime Errors

1. Trace exception source
2. Fix underlying architecture issue
3. Validate no regression introduced

## Refactor Conflicts

* Prefer consistency with existing architecture
* Refactor minimal necessary surface area
