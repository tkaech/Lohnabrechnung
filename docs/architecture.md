# Architektur

Die Loesung ist in vier Schichten getrennt:

- `Payroll.Domain`: Fachliche Kernobjekte wie Mitarbeiter, Arbeitszeiten, Spesen und Lohnlaeufe
- `Payroll.Application`: Anwendungslogik, Abfragen und Service-Abstraktionen
- `Payroll.Infrastructure`: EF Core, SQLite und technische Implementierungen wie Lohnrechner
- `Payroll.Desktop`: Avalonia-UI mit ViewModels, aber ohne Businesslogik

Abhaengigkeiten laufen nur nach innen:

- UI -> Application + Infrastructure
- Infrastructure -> Application + Domain
- Application -> Domain
- Domain -> keine

Damit bleibt die Businesslogik testbar und die Datenhaltung spaeter auf PostgreSQL erweiterbar.
