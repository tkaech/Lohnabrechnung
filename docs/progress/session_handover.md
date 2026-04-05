# Session Handover

## Fokus
Session-Abschluss nach Verlagerung der lohnrelevanten Zuschlagssaetze aus dem Mitarbeitendenbereich in einen zentralen Bereich `Einstellungen`

## Danach
Als naechstes die Monatserfassung in Richtung echter excel-artiger Mehrpersonenliste pro Monat weiterziehen; danach Vertragshistorie, Statusfluss und ersten Payroll-Orchestrierungsschritt sauber an den neuen Monatsanker anschliessen

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
- `EmployeeMonthlyRecord` als explizite Monatsanker-Entitaet in Domain, Application und Persistence umgesetzt
- referentielle Integritaet ueber eindeutigen Monatscontainer pro Mitarbeitenden und Monat sowie Child-Beziehungen fuer Zeiten, Spesen und Fahrzeugentschaedigung abgesichert
- Avalonia-UI um den ersten funktionalen Monatserfassungsbereich erweitert
- Monatserfassungs-UI danach neu zugeschnitten: Monat ist jetzt sichtbarer Fuehrungskontext, Personendaten liegen in einem separaten Tab statt vor den Erfassungsregistern
- Monatskontext wird jetzt bei Mitarbeitenden- oder Monatswechsel automatisch geladen; Speichern von Zeit- und Speseneintraegen haengt nicht mehr an einem separaten manuellen Lade-Schritt
- eigener Navigationsbereich `Zeit- und Spesenerfassung` umgesetzt; Mitarbeitenden-Stammdaten wieder als eigener Bereich abgetrennt
- ViewModel-Tests fuer Bereichsumschaltung und automatisches Nachladen des Monatskontexts ergänzt; voller Build- und Testlauf erfolgreich
- Tests fuer Monatskontext, Persistenz und ersten UI-nahen Arbeitsfluss ergänzt; voller Build- und Testlauf erfolgreich
- Fahrzeugentschaedigung im Monatserfassungsbereich von reiner Sichtbarkeit auf echten Bearbeitungsfluss erweitert
- `VehicleCompensation` kann jetzt im Monatskontext angelegt, ausgewaehlt, aktualisiert und geloescht werden
- gezielte Application- und ViewModel-Tests fuer diesen Flow ergänzt; Desktop-Build und fokussierter Testlauf ausserhalb der Sandbox erfolgreich
- Ursache fuer dauerhaft inaktive Save-Buttons im Monatsbereich identifiziert: lokale Development-DB war noch ein altes Schema ohne Monatstabellen
- Development-Bootstrap erstellt veraltete lokale DB-Schemata ohne Monatstabellen jetzt automatisch neu, damit der Monatskontext fuer Zeiten und Spesen wieder geladen werden kann
- Ursache fuer anschliessend weiter wirkungsloses Speichern identifiziert: kulturabhaengiges Datumsparsing im Monats-ViewModel akzeptierte ISO-Daten wie `2026-04-01` im de-CH-Kontext nicht stabil
- Monatsparser akzeptieren jetzt ISO- und lokale Datumsformate sowie gaengige Dezimaltrennzeichen; gezielter Test fuer diesen UI-nahen Fall erfolgreich
- danach verbleibenden EF-Concurrency-Fehler im Monatsbereich auf stale Tracking im langlebigen Desktop-`DbContext` eingegrenzt
- Monats-Repository und Service laden Aggregate vor Monatsoperationen jetzt nach `ChangeTracker.Clear()` frisch; fokussierter Monats-Testlauf erneut erfolgreich
- reproduzierbarer SQLite/ViewModel-Test fuer den echten Speicherpfad ergänzt und damit die verbleibende Root Cause sichtbar gemacht
- neue Child-Entitaeten im Monatsaggregat wurden mit vorvergebener GUID von EF als `Modified` statt `Added` behandelt; Service markiert neue Zeit-, Spesen- und Fahrzeugeintraege jetzt explizit als Inserts
- Monatsvorschau zeigt jetzt nicht mehr nur Summen, sondern eine tabellarische Monatsliste aller Eintraege des gewaehlten Monats untereinander
- Monatsvorschau wurde anschliessend auf alle vorhandenen Monate der selektierten Person erweitert und zeigt den Verlauf jetzt mit eigener Monatsspalte
- Spesenerfassung fachlich auf genau ein Monatstotal `Diverse Spesen` je `EmployeeMonthlyRecord` reduziert
- `ExpenseEntry` speichert jetzt nur noch Datum und Betrag; Typ/Beschreibung sind aus Domain, DTOs, Persistenz und UI entfernt
- Persistenzregel fuer genau einen Speseneintrag pro Monatskontext ergänzt; Preview und Payroll-Ableitung verwenden dafuer die feste fachliche Bezeichnung
- Domain-, Service-, ViewModel- und SQLite-Tests fuer die vereinfachte Spesenlogik angepasst und erweitert
- Nacht-, Sonntags- und Feiertagszuschlag aus dem Personenstamm entfernt und als zentrale Einstellungen fuer die spaetere Lohnberechnung umgesetzt
- `PayrollSettings` als kleiner zentraler Pflegeort fuer fixe Zuschlags-Prozentsaetze ergänzt; Mitarbeitendenformular zeigt diese Felder nicht mehr
- Payroll-Ableitung bezieht die drei Zuschlagssaetze jetzt fachlich aus zentralen Einstellungen statt aus individuellem Vertrags-/Stammdatenkontext
- Session sauber abgeschlossen und Übergabe fuer die nächste Session vorbereitet

## Kurzfassung
- Die gemeinsame Monatserfassung fuer Zeiten und Spesen ist als erster belastbarer Vertikalschnitt umgesetzt
- Fuehrender Kontext ist jetzt technisch und fachlich `EmployeeMonthlyRecord` pro Mitarbeitenden und Abrechnungsmonat
- Zeit-, Spesen- und Fahrzeugdaten sind referenziell an den Monatskontext gebunden; Fahrzeugentschaedigung bleibt fachlich getrennt, ist aber jetzt editierbar
- Spesen werden im Monatskontext jetzt nur noch als einzelnes Monatstotal `Diverse Spesen` erfasst
- Die Monatsvorschau bleibt bewusst einfach und zeigt Verdichtung statt vorweggenommener Payroll-Logik
- Die UI ist jetzt als eigener Erfassungsbereich benutzbar: Monat zuerst, Mitarbeitende darunter, rechts Erfassung fuer Zeiten, Spesen und Fahrzeugentschaedigung; Personendaten sind separat erreichbar

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
- der naechste groessere Bereich ist nicht mehr nur Sollbild, sondern ein implementierter Monatsanker mit erstem UI- und Service-Schnitt
- die naechste Session sollte diesen Schnitt mit Vertragshistorie, Statusfluss und erster Payroll-Orchestrierung zusammenfuehren

## Risiken
- Spezialfälle aus einzelnen Excel-Blättern können noch zusätzliche Payroll-Line-Typen oder Regeln erfordern
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
- Statusuebergaenge wie `Geprueft` oder `in Lohnlauf uebernommen` sind im Modell vorbereitet, aber fachlich noch nicht komplett durchdekliniert
- Die aktuelle Monatserfassung bleibt trotz stabilem Save-Flow noch eine Einzelperson-Sicht und noch keine echte Mehrpersonen-Monatstabelle
- fuer produktive oder langfristig persistierte Datenbanken fehlt weiterhin eine explizite Migrationsstrategie jenseits des automatischen Development-Rebuilds
- die Desktop-App verwendet weiterhin einen gemeinsam gehaltenen `DbContext`; die aktuelle Loesung entschärft das Monatsmodul, ersetzt aber noch keine grundsaetzliche Lifecycle-Bereinigung

## Bekannte Einschränkungen
- Keine Payroll-Berechnungserweiterung ueber die bisher dokumentierte Ableitungslogik hinaus
- Keine Reporting-, PDF- oder Secplan-Umsetzung in diesem Schnitt
- Vertragsverwaltung deckt weiterhin nur den aktuellen bzw. neuesten Vertragsstand ab
- Vertragshistorie ist erst fachlich vorbereitet, noch nicht als eigener Bearbeitungsfluss umgesetzt
- Die naechste UX-Stufe fuer den Bereich ist eine echte, excel-artige Monatsliste ueber mehrere Mitarbeitende

## Naechste 3 Schritte
- Die Monatserfassung in eine echte, excel-artige Mehrpersonenliste pro Monat weiterentwickeln
- Vertragshistorie als historisierte `EmploymentContract`-Versionen mit klaren Gueltigkeitsregeln konkretisieren
- danach den naechsten kleinen Payroll-Orchestrierungsschritt definieren, ohne offene Spezialfaelle zu automatisieren
