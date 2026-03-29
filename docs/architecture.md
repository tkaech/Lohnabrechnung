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

## Verbindliche Entwicklungsprinzipien

Zusaetzlich zur Schichtentrennung gelten folgende Grundsaetze:

- fachliche Logik nur einmal implementieren
- eine zentrale Quelle fuer Regeln, Konfiguration und Darstellungslogik
- konsistentes Verhalten und konsistente Darstellung in der gesamten UI
- keine Businesslogik in Views oder Code-Behind
- Wiederverwendung vor Neuerstellung

Die vollstaendige Beschreibung steht in [docs/development-principles.md](development-principles.md).
