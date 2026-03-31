# Architektur

## Schichten

### Domain
- Kernlogik
- Entitäten
- Value Objects
- keine Abhängigkeiten

### Application
- Use Cases
- Services
- Orchestrierung
- Validierung

### Infrastructure
- EF Core
- Datenbank
- Import
- PDF
- Logging

### UI
- Avalonia Views
- ViewModels
- keine Businesslogik

## Modulstruktur

- Employee
- TimeTracking
- Expenses
- Payroll
- AHV
- Tax
- Reporting

## Regeln

- Abhängigkeiten zeigen nach innen
- keine zirkulären Referenzen
- Module sind unabhängig
- Businesslogik nur in Domain/Application