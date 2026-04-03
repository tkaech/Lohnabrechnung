# Session Handover

## Fokus
Session-Abschluss nach Dokumentationsschritt fuer die kuenftige gemeinsame Monatserfassung von Zeiten und Spesen

## Danach
Als naechstes das dokumentierte Sollbild fuer die Monatserfassung gegen Vertragshistorie und Payroll-Orchestrierung sauber weiter zuschneiden

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
- Excel-Datei als Primärquelle für die Präzisierung offener Ableitungsregeln ausgewertet
- mehrdeutige Ueberlappung von Spezialstunden wird nun explizit als fachlicher Konflikt markiert
- weitere Spezialpositionen aus Excel identifiziert, aber bewusst noch nicht hart modelliert
- Employee- und EmploymentContract-Verwaltung als erster echter Funktionsschnitt umgesetzt
- EF Core / SQLite fuer Mitarbeitende und Verträge angebunden
- Avalonia-UI fuer Mitarbeiterliste plus Erfassungs-/Bearbeitungsformular umgesetzt
- Domain- und Application-Tests fuer Mitarbeitendenverwaltung ergänzt
- `Employee` um strukturierte Adresse, Kontaktdaten sowie vorbereitete payroll-relevante Stammdaten erweitert
- Employee-Repository und Application-Service fuer Suche/Filter nach Mitarbeitenden erweitert
- Avalonia-UI fuer Mitarbeitendenliste, Suchfeld, Aktiv/Inaktiv-Filter und erweitertes Stammdatenformular ausgebaut
- Domain- und Application-Tests fuer das erweiterte Stammdatenmodul ergänzt
- voller Solution-Testlauf ausserhalb der Sandbox erfolgreich ausgeführt
- Scroll-Verhalten der UI korrigiert, damit Liste und Formular auf normaler Bildschirmhoehe besser nutzbar bleiben
- Statusmeldung und Formularaktionen ausserhalb des scrollenden Formularbereichs fixiert
- lokale Entwicklungsdatenbank `payroll.localdev.db` eingeführt
- 10 Demo-Mitarbeitende mit plausiblen Stammdaten, Aktiv/Inaktiv-Zustaenden, Orten und payroll-nahen Merkmalen ergänzt
- Seed-Logik absichtlich nur fuer Development-Modus aktiviert und damit von produktiver Datenbank getrennt
- SQLite-inkompatible LINQ-Projektion in der Mitarbeitendenliste ersetzt
- neuester Vertragsstand wird fuer Liste und Speichern nun ueber getrennt geladenen Vertragsbestand im Speicher bestimmt
- SQLite-Repository-Test fuer Save+List ergänzt und erfolgreich ausgeführt
- Mitarbeitendenformular arbeitet nun standardmaessig im Ansichtsmodus
- explizite UI-Aktionen fuer `Neu`, `Bearbeiten`, `Speichern`, `Abbrechen` und `Loeschen` mit Sicherheitsabfrage ergänzt
- Loeschaktion fachlich bewusst als Archivieren/Deaktivieren statt physischem Entfernen umgesetzt
- vorbereitete Modulnavigation und konsistente Aktionsleistenstruktur fuer spaetere Bereiche ergänzt
- Ursache des Auswahl-Blockers eingegrenzt und behoben: waehrend asynchroner Detail-Ladevorgaenge wurde die Liste zu aggressiv deaktiviert und spaetere Selections gingen verloren
- Mitarbeitendenliste bleibt nun auch waehrend Detail-Ladevorgaengen selektierbar; spaetere Selection wird kontrolliert nachgezogen
- ViewModel-Tests fuer Initialauswahl, Selection waehrend Busy-Zustand, Abbrechen einer Neueingabe und Archivieren mit Reload ergänzt
- Archivieren ist im aktuellen UI-Flow nur noch innerhalb des Bearbeitungsmodus erreichbar
- die Bestaetigungsbuttons `Archivieren` und `Zurueck` werden nach dem Oeffnen der Sicherheitsabfrage jetzt korrekt aktiv
- Suche und Refresh bleiben auch im Bearbeitungsmodus verfuegbar; aktive Mitarbeitenden-Navigation ist nicht mehr deaktiviert
- busy-abhaengige Freigaben wie das Suchfeld aktualisieren sich nach Ladewechseln jetzt korrekt
- Vertragshistorie und Payroll-Orchestrierung fachlich fuer die naechsten Schritte dokumentiert
- Planungsdokumente an den realen Projektstand angepasst
- Projektkontext, Roadmap, Modulgrenzen, Datenmodell, UI-Design-System und AI-Zusammenfassung um das Sollbild `Monatserfassung pro Mitarbeitenden` ergänzt
- manuelle Erfassung vor Import, gemeinsamer Monatskontext fuer Zeiten und Spesen sowie Monatsvorschau als Verdichtung explizit dokumentiert
- Jahresdarstellung und Lohnblatt als spaetere Ableitung aus Monatsdaten festgehalten
- Session sauber abgeschlossen und Übergabe fuer die nächste Session vorbereitet

## Kurzfassung
- Die gemeinsame Monatserfassung fuer Zeiten und Spesen ist jetzt als naechster groesserer fachlicher Bereich dokumentiert
- Fuehrender Kontext ist kuenftig Mitarbeitender plus Abrechnungsmonat
- Manuelle Erfassung kommt vor Import; Jahresdarstellungen und Lohnblaetter werden spaeter aus Monatsdaten abgeleitet
- Monatsvorschau ist als Verdichtung und nicht als Rohdatenerfassung beschrieben

## Regeln
- keine UI Logik
- keine Duplikate

## Projektstand
- Domain ist nicht mehr nur ein Platzhalter, sondern bildet die Kernregeln aus der Excel-Analyse explizit ab
- Zuschlagsbewertung, Spesenarten, BVG-Herkunft und Quellensteuer-Tariflogik sind nun fachlich explizit beschrieben
- PayrollRunLine ist nun als auditierbare Ergebniszeile mit direkter oder berechneter Herkunft modelliert
- gesicherte Excel-Hinweise, Annahmen und weiterhin offene Regeln der Payroll-Ableitung sind nun sauber getrennt dokumentiert
- Die Application-Schicht hat jetzt erstmals echte CRUD-Logik im Employee-Modul
- Die UI zeigt erstmals echte Daten und Formulare, bleibt aber bewusst auf Mitarbeitende und den aktuellen Vertragsstand begrenzt
- Kontakt-, Adress- und vorbereitete payroll-relevante Personendaten sind nun im Employee-Modul verankert, ohne schon starre Fachcodes zu erzwingen
- Suche und Filter fuer Mitarbeitende laufen ueber Application und Repository, nicht ueber UI-Logik
- lokale Demo-Daten sind bewusst eine Entwicklungsfunktion und werden ueber eigene Datenbankdatei plus Environment-Steuerung vom produktiven Betrieb getrennt
- Demo-Daten werden in `EmployeeDevelopmentDataSeeder` erzeugt; im Development-Modus verwendet die Desktop-App dafuer `payroll.localdev.db`, sonst `payroll.db`
- fuer spaetere Bereiche ist nun auch die UI-Fuehrung klarer festgelegt: Navigation plus konsistente Aktionsleiste
- fuer den naechsten groesseren Bereich ist nun die gemeinsame Monatserfassung pro Mitarbeitenden als Sollbild dokumentiert
- die naechste Session sollte dieses Sollbild mit Vertragshistorie und erster Payroll-Orchestrierung zusammenfuehren

## Risiken
- Spezialfälle aus einzelnen Excel-Blättern können noch zusätzliche Payroll-Line-Typen oder Regeln erfordern
- fuer die Monatserfassung ist noch offen, ob der Monatskontext spaeter eine eigene explizite Entitaet benoetigt
- Ueberlappung von Nacht-, Sonntags- und Feiertagsstunden ist fachlich noch offen, wird aber im Modell jetzt als Konflikt sichtbar gemacht
- Pro-Rata-Behandlung des fixen BVG-Betrags bei Teilmonaten ist noch offen
- weitere Spezialpositionen wie Fahrzeitentschädigung, Mehrzeit/Unterzeit, Weiterbildung und Unfalltaggeld sind identifiziert, aber noch nicht in der Ableitung umgesetzt
- Die Mitarbeitenden-UI bearbeitet derzeit nur den neuesten Vertragsstand; Historisierung ist noch kein eigener UI-Flow
- fuer Bewilligung, Steuerstatus und Quellensteuer-Merkmale fehlen noch final entschiedene fachliche Codes und Regelanknuepfungen
- Wenn spaeter weitere Umgebungen dazukommen, sollte die Environment-Steuerung fuer Demo-Seeds zentraler konfigurierbar gemacht werden
- Die aktuelle Listenlogik ist bewusst simpel und provider-sicher; bei deutlich mehr Mitarbeitenden sollte ihre Performance erneut beurteilt werden
- die aktuelle Loeschstrategie ist bewusst konservativ; wenn kuenftig abgestufte Archivierungsregeln je Datenabhaengigkeit noetig werden, muessen diese fachlich weiter differenziert werden
- `dotnet test` ist in dieser Sandbox weiterhin durch eine Socket-Einschraenkung technisch limitiert
- Trotz ViewModel-Tests sollte der kombinierte UI-Flow in Avalonia noch einmal manuell gegengeprueft werden

## Bekannte Einschränkungen
- Keine Payroll-Berechnungserweiterung ueber die bisher dokumentierte Ableitungslogik hinaus
- Keine Reporting-, PDF- oder Secplan-Umsetzung in diesem Schnitt
- gemeinsame Monatserfassung fuer Zeiten und Spesen ist bisher nur dokumentiert, nicht umgesetzt
- Vertragsverwaltung deckt weiterhin nur den aktuellen bzw. neuesten Vertragsstand ab
- Vertragshistorie ist erst fachlich vorbereitet, noch nicht als eigener Bearbeitungsfluss umgesetzt

## Naechste 3 Schritte
- Vertragshistorie als historisierte `EmploymentContract`-Versionen mit klaren Gueltigkeitsregeln konkretisieren
- ersten fachlichen Umsetzungschnitt fuer die gemeinsame Monatserfassung zuschneiden: Zeiten, Spesen und Monatsvorschau im Monatskontext
- danach den naechsten kleinen Payroll-Orchestrierungsschritt definieren, ohne offene Spezialfaelle zu automatisieren
