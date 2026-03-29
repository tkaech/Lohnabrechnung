# TODO

## MVP

- Implement employee management UI for listing, creating, and editing employee master data
- Add EF Core migrations and a reproducible local database setup
- Implement CSV import for working hours with validation and import preview
- Extend payroll calculation with configurable Swiss deduction rates
- Add expense entry and manual adjustment workflow
- Generate monthly payroll runs from imported hours and expenses
- Create PDF payslips for a completed payroll run

## Architecture

- Introduce application commands and DTOs for write operations
- Add repository-independent tests for payroll calculation logic
- Move payroll rates and company settings into configurable persistence
- Prepare infrastructure abstractions for future PostgreSQL support

## UI

- Create navigation for Employees, Hours, Expenses, Payroll Runs, and Settings
- Add forms with validation messages and user-friendly error handling
- Improve the dashboard beyond the current starter table

## Operations

- Add a proper `.editorconfig`
- Add automated tests to the solution
- Add CI workflow for build and test on GitHub Actions
- Document the setup and release process
