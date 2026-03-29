# Wichtigste Klassen

- `Employee`: Stammdaten eines Mitarbeiters inklusive Lohnart und Basisverguetung
- `ImportedWorkTime`: Importierte Arbeitsstunden aus CSV
- `ExpenseClaim`: Spesen oder manuelle Anpassungen
- `PayrollRun`: Monatslauf fuer einen bestimmten Monat
- `PayrollEntry`: Berechnetes Resultat pro Mitarbeiter innerhalb eines Monatslaufs

Technische Kernbausteine:

- `AppDbContext`: EF-Core-Kontext
- `PayrollRunService`: orchestriert die monatliche Lohnberechnung
- `SwissPayrollCalculator`: enthaelt die eigentliche Lohnlogik
- `EmployeeQueries`: liest Daten fuer die UI ohne Fachlogik in Views
