# Swiss Payroll Management

Desktop application for payroll management in Switzerland built with C#, .NET 8, Avalonia, SQLite, and Entity Framework Core.

The project follows a layered architecture with a clear separation between domain model, business logic, infrastructure, and UI. It is designed as a local, maintainable foundation for small to medium-sized teams with around 40 employees and can later be extended for PostgreSQL and additional payroll rules.

## Goals

The application is intended to support monthly payroll processing in a Swiss business context and provide a solid technical base for future functional growth.

Planned core features:

- Employee master data management
- CSV import for working hours
- Payroll calculation including Swiss deductions such as AHV and ALV
- Expense management with manual adjustments
- Monthly payslip generation as PDF

## Project Structure

- `src/Payroll.Domain`: Core business entities and rules
- `src/Payroll.Application`: Use cases, queries, and business orchestration
- `src/Payroll.Infrastructure`: EF Core, SQLite, and technical implementations
- `src/Payroll.Desktop`: Avalonia desktop UI and view models
- `docs`: Architecture notes and project planning

## Development Principles

The project follows a few non-negotiable implementation rules:

- business logic should exist only once and not be duplicated
- the same kind of data should always be presented consistently
- UI behavior should remain predictable across all screens
- UI must not contain business logic
- existing code should be reused before creating parallel structures

See [docs/development-principles.md](docs/development-principles.md) for the full project rules.

## Tech Stack

- Language: C#
- Framework: .NET 8
- UI: Avalonia
- Database: SQLite
- ORM: Entity Framework Core
- IDE: VS Code

## Getting Started

```powershell
dotnet build Lohnabrechnung.slnx
dotnet run --project src/Payroll.Desktop/Payroll.Desktop.csproj
```

## Current Status

The repository currently contains a minimal starter project with:

- a clean layered architecture
- initial domain models for employees, work time, expenses, and payroll runs
- an EF Core SQLite data context
- a minimal Avalonia desktop UI with seeded sample employees

The current state is intended as a technical foundation for the next functional milestones.

## Roadmap

- CSV import for working hours
- configurable payroll deduction rates
- PDF generation for payslips
- preparation for future PostgreSQL support
