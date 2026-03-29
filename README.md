# Lohnabrechnung Schweiz

Desktop-Anwendung fuer die Lohnverwaltung in der Schweiz auf Basis von C#, .NET 8, Avalonia, SQLite und Entity Framework Core.

## Ziel

Die Anwendung soll kleinere bis mittlere Teams mit rund 40 Mitarbeitenden bei der monatlichen Lohnverarbeitung unterstuetzen.

Geplante Kernfunktionen:

- Mitarbeiter-Stammdaten verwalten
- Arbeitsstunden per CSV importieren
- Lohnberechnung inklusive Schweizer Abzuegen wie AHV und ALV
- Spesen und manuelle Anpassungen erfassen
- Monatliche Lohnabrechnungen als PDF erzeugen

## Projektstruktur

- `src/Payroll.Domain`: Fachliche Kernobjekte
- `src/Payroll.Application`: Anwendungslogik und Abfragen
- `src/Payroll.Infrastructure`: EF Core, SQLite und technische Implementierungen
- `src/Payroll.Desktop`: Avalonia-UI mit ViewModels
- `docs`: Architektur- und Projektnotizen

## Technik

- Sprache: C#
- Framework: .NET 8
- UI: Avalonia
- Datenbank: SQLite
- ORM: Entity Framework Core
- Entwicklungsumgebung: VS Code

## Starten

```powershell
dotnet build Lohnabrechnung.slnx
dotnet run --project src/Payroll.Desktop/Payroll.Desktop.csproj
```

## Status

Das Repository enthaelt aktuell ein minimales Startprojekt mit:

- sauber getrennten Schichten
- vorbereiteten Domain-Klassen fuer Mitarbeiter, Stunden, Spesen und Lohnlauf
- EF-Core-Kontext fuer SQLite
- minimaler Avalonia-Oberflaeche mit Beispielmitarbeitern

## Naechste Schritte

- CSV-Import fuer Arbeitsstunden
- konfigurierbare Abzugssaetze
- PDF-Erstellung fuer Lohnabrechnungen
- spaetere Vorbereitung fuer PostgreSQL
