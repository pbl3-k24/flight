# PROJECT CONTEXT

Project: Flight Booking System

## Domain

Online flight booking platform supporting:

* User authentication / authorization
* Flight search and filtering
* Seat inventory management
* Booking and payment
* Refund and cancellation
* Promotions / discounts
* Ticket issuance
* Notifications / audit logs

## Core Actors

* Guest
* Customer
* Staff
* Manager
* Admin

## Key Business Rules

* Booking may include outbound and optional return flight
* Seats are limited per flight / seat class
* Promotions may expire / have usage limits
* Refunds depend on refund policy
* Payment required before booking confirmation
* Fare at booking time must be preserved historically

## Technical Stack

* Backend: ASP.NET Core + EF Core + PostgreSQL
* Frontend: SPA Client
* Architecture: Layered / Clean-inspired

AI must preserve business consistency across all generated code.
