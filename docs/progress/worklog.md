# Worklog

## Start
Projekt neu strukturiert

## 2026-03-31
- Solution- und Projektstruktur unter `src/` und `tests/` erstellt
- Schichten gemäss Architektur getrennt
- Modulordner für Employee, TimeTracking, Expenses, Payroll, AHV, Tax und Reporting angelegt
- erste Basisentitäten und Service-Platzhalter erstellt
- lokale `.sln` für .NET 8 ergänzt

## 2026-04-03
- `prompts/codex_start.md` befolgt und Projektkontext aus `docs/context`, `docs/planning` und `docs/progress` eingearbeitet
- Excel-Analyse unter `docs/analysis/` mit bestehendem Domain-Modell abgeglichen
- `Employee` mit Validierung, `FullName` und Umbenennung ergänzt
- `EmploymentContract` neu eingeführt mit Stundenansatz, Gültigkeit und fixem monatlichem BVG-Betrag
- `TimeEntry` auf `decimal`-Stunden plus Zuschlagsstunden geschärft
- `ExpenseEntry` auf CHF-Betrag präzisiert und `VehicleCompensation` als zentrale Entität ergänzt
- `PayrollRun` um Status, Linienaggregation sowie Summen- und Stundenlogik erweitert
- `PayrollRunLine` inklusive Typen und Fabrikmethoden für Stunden-, CHF- und fixe Abzugszeilen ergänzt
- `ContributionRate` und `TaxRule` um versionierbare Gültigkeit und Berechnungslogik ergänzt
- xUnit-Testprojekte vervollständigt und Unit Tests für Domainlogik ergänzt
- `dotnet build Lohnabrechnung.sln -maxcpucount:1 -nodeReuse:false` erfolgreich ausgeführt
- `dotnet test Lohnabrechnung.sln -maxcpucount:1 -nodeReuse:false` kompiliert, Testausführung aber durch Sandbox-Socket-Restriktion blockiert
- offene Fachfragen aus `docs/analysis/open_questions.md` entschieden und in Domain-Entscheidungen sowie Mapping-Dokumentation überführt
- fachliche Definition von `PayrollRunLine` als auditierbare Ergebniszeile dokumentiert
- Ableitungslogik aus Arbeitsstunden, Nacht-/Sonntags-/Feiertagsstunden, Spesen, Fahrzeugentschädigung und Vertragsdaten modelliert
- `TimeEntry` um Nacht-, Sonntags- und Feiertagsstunden geschärft
- `EmploymentContract` um konfigurierbare Zuschlagsparameter ergänzt
- `ExpenseEntry` um fachlichen Spesentypcode ergänzt
- `PayrollRunLine` um Herkunft direkt vs. berechnet ergänzt
- `PayrollRunLineDerivationService`, `PayrollWorkSummary` und Ergebnis-Issues für offene Regeln ergänzt
- `dotnet build Lohnabrechnung.sln -maxcpucount:1 -nodeReuse:false` nach den Änderungen erfolgreich ausgeführt
- `dotnet test Lohnabrechnung.sln -maxcpucount:1 -nodeReuse:false` weiterhin nur an der Sandbox-Socket-Restriktion gescheitert
