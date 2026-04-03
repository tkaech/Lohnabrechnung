# Status

Projekt wird neu aufgebaut und das Domain-Modell wird schrittweise aus der Excel-Analyse abgeleitet.

## Fix
- Stack definiert
- Architektur definiert
- Projektstruktur erstellt
- Basisprojekte für Domain, Application, Infrastructure und Desktop angelegt
- Teststruktur für Domain und Application angelegt
- Excel-Analyse in Domain-Entscheidungen und Mapping überführt
- Domain-Modell für Employee, EmploymentContract, TimeEntry, ExpenseEntry, VehicleCompensation, PayrollRun, PayrollRunLine, ContributionRate und TaxRule geschärft
- Einheiten konsolidiert: Stunden als `decimal`, Zuschläge als Stunden, Spesen in CHF, BVG als fixer CHF-Betrag, Fahrzeugentschädigung zentral
- Offene Fachfragen zu Zuschlägen, Spesenarten, BVG, Quellensteuer und Secplan-Domain-Abgrenzung entschieden und dokumentiert
- Ableitungslogik für PayrollRunLine fachlich definiert und als kleines Domain-Modell mit Derivation-Service umgesetzt
- PayrollRunLine trennt jetzt direkte Übernahmen und berechnete Werte explizit
- Arbeitsstunden sowie Nacht-, Sonntags- und Feiertagsstunden sind als getrennte Eingaben für die Payroll-Ableitung modelliert
- Unit Tests für Domainlogik ergänzt
- Solution Build erfolgreich

## Offen
- Fachliche Details aus Spezialfällen der Excel-Datei weiter präzisieren
- Payroll-Orchestrierung in der Application-Schicht auf das neue Domain-Modell ausrichten
- Avalonia und EF Core Pakete integrieren
- Secplan-Import fachlich klären

## Risiken
- Excel enthält verteilte Speziallogik; einzelne Ausnahmefälle aus Unfall-, KTG- oder Sondertabellen können im aktuellen Domain-Modell noch fehlen
- Überlappung oder Kumulation von Nacht-, Sonntags- und Feiertagsstunden ist fachlich noch nicht abschliessend geklärt
- Pro-Rata-Regeln für fixen BVG bei Teilmonaten sind noch offen
- Testausführung ist in der aktuellen Sandbox blockiert, weil der Test-Runner keine lokalen Sockets öffnen darf
