# Contributing

This project follows a few mandatory engineering rules to keep the codebase maintainable, predictable, and extensible.

## Core Rules

- Do not duplicate business logic.
- Reuse existing components, services, and patterns before creating new ones.
- Keep a single source of truth for calculations, configuration, validation, and formatting rules.
- Keep UI behavior and data presentation consistent across the application.
- Do not place business logic in views, code-behind, or UI-only classes.

See [docs/development-principles.md](docs/development-principles.md) for the detailed project principles.

## Architecture Expectations

- `Payroll.Domain` contains core business entities and domain rules.
- `Payroll.Application` contains use cases, orchestration, and application services.
- `Payroll.Infrastructure` contains persistence and technical implementations.
- `Payroll.Desktop` contains UI, view models, and presentation logic only.

Dependencies should always point inward toward the domain and application core.

## Before Adding New Code

- Check whether similar functionality already exists.
- Extend existing logic where appropriate instead of creating parallel implementations.
- Prefer central shared services or components over local one-off solutions.
- Keep formatting and display behavior aligned with existing patterns.

## Review Checklist

- Is the same business rule implemented in more than one place?
- Is any business logic leaking into the UI layer?
- Does the new code introduce another source of truth for an existing rule?
- Will the new UI behave the same way as similar existing screens?
- Can an existing class, service, or component be reused instead?

## Quality Expectations

- Keep classes focused and responsibilities clear.
- Prefer explicit names over vague abstractions.
- Add tests for reusable or sensitive business logic.
- Keep documentation aligned when architectural or behavioral rules change.
