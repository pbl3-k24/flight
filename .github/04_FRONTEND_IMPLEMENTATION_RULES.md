# FRONTEND IMPLEMENTATION RULES

## Architecture

Use feature-based folder structure.

## API Access

* Centralize API calls in service layer
* Never call fetch/axios directly inside UI components

## State Management

* Local state for isolated UI
* Shared/global store for auth/cart/booking flow

## UX Rules

* Handle loading / error / empty states
* Validate forms client-side before submit

## Component Rules

* Keep components small and focused
* Extract reusable UI patterns
