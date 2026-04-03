# Status

Projekt wird neu aufgebaut und das Domain-Modell wird schrittweise aus der Excel-Analyse abgeleitet.

## Session Abschluss
- fachliches Sollbild fuer die kuenftige gemeinsame Monatserfassung von Zeiten und Spesen dokumentarisch verankert
- Monatskontext pro Mitarbeitenden als naechster groesserer Arbeitsbereich in Kontext-, Planungs- und Progress-Dokumenten konsistent beschrieben
- manuelle Erfassung vor Import sowie Ableitung von Jahresdarstellungen aus Monatsdaten explizit festgehalten
- Monatsvorschau als Verdichtung und nicht als Rohdatenerfassung dokumentiert
- Mitarbeitenden-UI in mehreren kleinen Schritten stabilisiert: Auswahl, Delete-Bestaetigung, Such-/Refresh-Freigaben und aktive Navigation funktionieren jetzt konsistent
- Archivieren ist nur noch im Bearbeitungsmodus erreichbar; Sicherheitsabfrage und ihre Buttons sind funktionsfaehig
- Suchfeld, Filter und Refresh sind wieder benutzbar und aktualisieren ihre Busy-abhaengigen Freigaben korrekt
- ViewModel-Tests fuer kritische UI-Zustaende erweitert; Solution-Build erneut erfolgreich verifiziert
- Busy-abhaengige UI-Bindings im Mitarbeitenden-ViewModel aktualisieren jetzt ihre IsEnabled-Zustaende korrekt nach Ladewechseln
- Suche und Refresh der Mitarbeitendenliste sind jetzt auch waehrend des Bearbeitungsmodus verfuegbar
- aktive Navigationsbuttons fuer die reale Mitarbeitendenseite werden nicht mehr kuenstlich deaktiviert
- Delete-Bestaetigungsbuttons im Bearbeitungsmodus wieder aktiv geschaltet; fehlendes Command-Refresh beim Einblenden der Sicherheitsabfrage behoben
- Archivieren ist in der Mitarbeitendenverwaltung jetzt nur noch aus dem Bearbeitungsmodus heraus möglich
- UI-Blocker in der Mitarbeitendenauswahl analysiert; Ursache in asynchronem Selection-Handling und zu aggressivem Disable-Zustand der Liste eingegrenzt und behoben
- Employee-Flow im ViewModel gezielt gegen instabile Zustandswechsel abgesichert
- ViewModel-Tests fuer Initialauswahl, Selections waehrend Busy-Zustand, Abbrechen nach Neueingabe und Archivieren mit Reload ergänzt
- Vertragshistorie fachlich als versionierbarer Vertragsstand präzisiert und naechste Payroll-Orchestrierung auf Basis der bestehenden Domain-Ableitung dokumentiert
- Planungsdokumente an den tatsächlichen Projektstand angeglichen
- PayrollRunLine-Ableitung fachlich präzisiert und offene Regeln explizit markiert
- Erster vertikaler Funktionsschnitt für Mitarbeitendenverwaltung von Domain über Application und Infrastructure bis zur Avalonia-UI umgesetzt
- Build der gesamten Solution erfolgreich verifiziert
- Mitarbeitendenverwaltung zu einem nutzbaren Stammdatenmodul mit erweiterten Personendaten, strukturierter Adresse, Suche/Filter und bearbeitbarer Payroll-relevanter Stammdatenvorbereitung ausgebaut
- Domain- und Application-Tests für das erweiterte Stammdatenmodul ergänzt und erfolgreich ausgeführt
- UI der Mitarbeitendenverwaltung für normale Bildschirmhöhen benutzbarer gemacht und lokale Demo-Mitarbeitende für Entwicklung ergänzt
- SQLite-Fehler beim Speichern und anschliessenden Laden der Mitarbeitendenliste analysiert und durch SQLite-kompatible Query-Pfade behoben
- Mitarbeitendenverwaltung zu einer klaren Arbeitsoberflaeche mit Ansichtsmodus, explizitem Bearbeiten, konsistenter Aktionsleiste und vorbereiteter Modulnavigation umgebaut
- Session-Abschluss und Übergabe erstellt; aktueller UI-Blocker fuer die nächste Session klar markiert

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
- Excel-basierte Präzisierung der offenen Payroll-Regeln vorgenommen: NSF als Sammelbegriff bestätigt, weitere Spezialpositionen identifiziert, offene Regeln explizit getrennt
- Mehrdeutige Ueberlappung von Spezialstunden wird jetzt fachlich erkannt und nicht stillschweigend berechnet
- Erster vertikaler Funktionsschnitt für Mitarbeitendenverwaltung umgesetzt
- Domain für `Employee` und `EmploymentContract` um Bearbeitungslogik ergänzt
- Application-Service für Mitarbeitende auflisten, anlegen und bearbeiten umgesetzt
- EF-Core-/SQLite-Persistenz für Mitarbeitende und Verträge angebunden
- Einfache Avalonia-UI für Mitarbeiterliste und Erfassungs-/Bearbeitungsformular umgesetzt
- Unit Tests für Domainlogik ergänzt
- Application-Tests für Mitarbeitendenverwaltung ergänzt
- Solution Build erfolgreich
- `Employee` um weitere Stammdaten, Kontaktfelder und vorbereitete payroll-relevante Personenmerkmale ergänzt
- Adressdaten als eigene strukturierte Komponente modelliert
- EF-Core-Mapping für erweiterte Mitarbeitendenstammdaten und Adresse ergänzt
- Employee-Repository unterstützt jetzt Suche nach Mitarbeitenden sowie Filter nach aktiv/inaktiv
- Avalonia-UI für Mitarbeitendenverwaltung um Suchfeld, Aktivitätsfilter und erweitertes Stammdatenformular ergänzt
- Domain-Tests für Employee-Profilvalidierung ergänzt
- Application-Tests für Suche/Filter und erweitertes Speichern ergänzt
- `dotnet test Lohnabrechnung.sln -maxcpucount:1 -nodeReuse:false` ausserhalb der Sandbox erfolgreich ausgeführt
- Scroll-Verhalten der Mitarbeitendenverwaltung über Card-Template und Formularlayout verbessert, so dass Liste und Formular auf normaler Bildschirmhöhe nutzbar bleiben
- Statusbereich und Aktionsleiste im Formular fixiert, während die Felder separat scrollen
- Lokale Entwicklungsdatenbank `payroll.localdev.db` mit 10 Demo-Mitarbeitenden ergänzt
- Demo-Daten bewusst von produktiver Datenbank `payroll.db` getrennt
- Seed-Test für lokale Demo-Daten ergänzt
- SQLite-inkompatible Listenabfrage mit `OrderByDescending(...).FirstOrDefault()` innerhalb einer Projektion entfernt
- Mitarbeitendenliste lädt jetzt zuerst Mitarbeitende und bestimmt den neuesten Vertragsstand danach im Speicher
- Speichern nutzt für die Ermittlung des neuesten Vertrags ebenfalls einen robusten, SQLite-kompatiblen Ladepfad
- zusätzlicher Repository-Test gegen echte SQLite-In-Memory-Datenbank ergänzt
- Formular arbeitet jetzt standardmaessig im Ansichtsmodus; Bearbeitung startet erst ueber `Neu` oder `Bearbeiten`
- expliziter Editierfluss `Neu`, `Bearbeiten`, `Speichern`, `Abbrechen` ergänzt
- Loeschfunktion mit Sicherheitsabfrage umgesetzt und fachlich als Archivieren/Deaktivieren statt physischem Loeschen modelliert
- Archivieren ist jetzt bewusst an den Bearbeitungsmodus gekoppelt und nicht mehr direkt aus der reinen Ansicht heraus möglich
- Navigationsstruktur mit klaren Platzhaltern fuer Zeiten, Spesen, Lohnlaeufe, AHV/Abzuege, Quellensteuer, Reporting und Einstellungen vorbereitet
- UI-Design-System fuer Shell-Struktur und Aktionsleisten konkretisiert

## Offen
- konkreter fachlicher Zuschnitt der ersten manuellen Monatserfassung gegen bestehendes Domain-Modell abgleichen
- Vertragshistorie als historisierte `EmploymentContract`-Versionen fachlich konkretisieren
- Ersten kleinen Payroll-Orchestrierungsschritt in der Application-Schicht definieren
- Fachliche Details aus Spezialfällen der Excel-Datei weiter präzisieren
- Payroll-Orchestrierung in der Application-Schicht auf das neue Domain-Modell ausrichten
- Secplan-Import fachlich klären
- Fachliche Codierung und Versionierung für Bewilligung, Steuerstatus und Quellensteuer-Merkmale präzisieren
- Optional: erweiterte Listenfunktionen wie Sortierung oder kompaktere Kartenansicht prüfen, falls die Mitarbeitendenzahl weiter wächst
- Optional: Repository-Queries bei wachsender Datenmenge weiter optimieren, ohne wieder provider-spezifische LINQ-Muster einzuführen
- Weitere Bereiche schrittweise auf die neue Shell- und Aktionsstruktur aufsetzen

## Risiken
- Excel enthält verteilte Speziallogik; einzelne Ausnahmefälle aus Unfall-, KTG- oder Sondertabellen können im aktuellen Domain-Modell noch fehlen
- fuer die kuenftige Monatserfassung ist noch offen, ob der Monatskontext spaeter eine eigene explizite Entitaet benoetigt oder implizit ueber Mitarbeitendenbezug plus Monat gefuehrt wird
- Überlappung oder Kumulation von Nacht-, Sonntags- und Feiertagsstunden ist fachlich weiterhin nicht abschliessend geklärt; das System markiert solche Fälle jetzt explizit
- Pro-Rata-Regeln für fixen BVG bei Teilmonaten sind noch offen
- Weitere Excel-Spezialpositionen wie Fahrzeitentschädigung, Mehrzeit/Unterzeit, Weiterbildung und Unfalltaggeld sind identifiziert, aber noch nicht in die Ableitung integriert
- Die UI bearbeitet weiterhin nur den aktuellen bzw. neuesten Vertragsstand eines Mitarbeitenden, noch keine volle Vertragshistorie
- Mehrere neue payroll-relevante Personendaten sind bewusst als vorbereitete Freitext-/Optionsfelder modelliert; die fachliche Codierung ist noch nicht abschliessend entschieden
- Demo-Daten werden absichtlich nur im lokalen Development-Modus gesät; für andere Umgebungen braucht es weiterhin echte oder separat bereitgestellte Daten
- Die aktuelle SQLite-kompatible Lösung bevorzugt Robustheit vor maximaler Query-Kompaktheit und lädt Vertragsstände für Listen bewusst separat
- Die neue Mitarbeitenden-Loeschaktion archiviert bewusst nur logisch; falls spaeter echte Archivierungsregeln je Datenabhaengigkeit noetig werden, muessen diese explizit erweitert werden
- `dotnet test` bleibt in der Sandbox weiterhin durch eine Socket-Einschraenkung blockiert; neue ViewModel-Tests konnten deshalb lokal nur bis zum erfolgreichen Build verifiziert werden

## Bekannte Einschränkungen
- Mitarbeitendenverwaltung deckt aktuell Stammdaten, Kontakt, Adresse und einen bearbeitbaren Vertragsstand ab
- gemeinsame Monatserfassung fuer Zeiten und Spesen ist bislang nur als fachliches Sollbild dokumentiert
- Noch keine vollstaendige Payroll-Orchestrierung auf Basis der vorhandenen Domainregeln
- Vertragshistorie ist fachlich vorbereitet, aber noch nicht als eigener Bearbeitungsfluss umgesetzt
- `dotnet test` ist in dieser Sandbox weiterhin nicht verlässlich lokal ausfuehrbar
