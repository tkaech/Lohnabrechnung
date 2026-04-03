# Session Handover

## Fokus
Domain-Modell aus Excel-Analyse geschärft, offene Fachfragen entschieden und PayrollRunLine-Ableitung definiert

## Danach
Application-Services und Persistenz auf die neue Payroll-Ableitung ausrichten

## Erledigt
- Projektstruktur erstellt
- Basis-Solution fuer .NET 8 angelegt
- Schichten und Modulordner vorbereitet
- Excel-Analyse in konkrete Domain-Entscheidungen überführt
- `Employee`, `EmploymentContract`, `TimeEntry`, `ExpenseEntry`, `VehicleCompensation`, `PayrollRun`, `PayrollRunLine`, `ContributionRate` und `TaxRule` fachlich ergänzt oder korrigiert
- Stunden und Zuschläge als `decimal`-Stunden modelliert
- Spesen als CHF-Beträge und BVG als fixer CHF-Abzug modelliert
- Fahrzeugentschädigung zentral als eigene Entität modelliert
- Unit Tests für Domainlogik ergänzt
- Solution Build erfolgreich verifiziert
- offene Fachfragen zu Zuschlägen, Spesenarten, BVG, Quellensteuer und Secplan-Domain-Abgrenzung entschieden und dokumentiert
- fachliche Definition und Ableitungslogik von `PayrollRunLine` dokumentiert
- `TimeEntry` um Nacht-, Sonntags- und Feiertagsstunden erweitert
- `EmploymentContract` um konfigurierbare Zuschlagsparameter erweitert
- `ExpenseEntry` um Spesentypcode erweitert
- `PayrollRunLineDerivationService` und `PayrollWorkSummary` als schlanke Derivation-Schicht ergänzt
- Domain-Tests für Payroll-Ableitung ergänzt; Test-Runner weiterhin sandbox-limitiert

## Regeln
- keine UI Logik
- keine Duplikate

## Projektstand
- Domain ist nicht mehr nur ein Platzhalter, sondern bildet die Kernregeln aus der Excel-Analyse explizit ab
- Zuschlagsbewertung, Spesenarten, BVG-Herkunft und Quellensteuer-Tariflogik sind nun fachlich explizit beschrieben
- PayrollRunLine ist nun als auditierbare Ergebniszeile mit direkter oder berechneter Herkunft modelliert
- Die Application-Schicht ist weiterhin weitgehend leer und muss die neue Ableitungslogik im nächsten Schritt orchestrieren

## Risiken
- Spezialfälle aus einzelnen Excel-Blättern können noch zusätzliche Payroll-Line-Typen oder Regeln erfordern
- Ueberlappung von Nacht-, Sonntags- und Feiertagsstunden ist fachlich noch offen
- Pro-Rata-Behandlung des fixen BVG-Betrags bei Teilmonaten ist noch offen
- Testausführung ist lokal in dieser Sandbox blockiert, weil `dotnet test` keine Socket-Kommunikation initialisieren darf

## Empfohlene nächste Schritte
- Offene Payroll-Regeln zu Stundenueberlappung, BVG-Pro-Rata und Spezialfaellen fachlich entscheiden
- Application-Services auf die neue Ableitungslogik und Ergebnis-Issues ausrichten
- fehlende fachliche Klassifikationen wie Spesenart und weitere Zuschlagsarten gezielt modellieren
- EF-Core-Mappings für die neuen Entitäten planen, ohne Businesslogik aus der Domain zu verlagern
