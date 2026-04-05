# AI Summary

## Projekt
Payroll Desktop App (Schweiz)

## Ziel
Entwicklung einer Desktop-Anwendung zur Lohnverwaltung für ca. 40 Mitarbeitende mit Fokus auf Schweizer Anforderungen (AHV, ALV, BVG, Quellensteuer, Lohnausweis).

## Technologie
- C#
- .NET 8
- Avalonia UI
- EF Core
- SQLite (Start, später PostgreSQL möglich)

## Architektur
- Schichten:
  - Domain
  - Application
  - Infrastructure
  - UI
- Modular:
  - Employee
  - TimeTracking
  - Expenses
  - Payroll
  - AHV
  - Tax
  - Reporting

---

# Aktueller Stand

- Excel-Datei fachlich analysiert
- PayrollRunLine-Ableitung modelliert
  - Trennung zwischen Direct und Calculated
  - offene Regeln bewusst nicht hardcodiert
- erster vertikaler Schnitt umgesetzt und stabilisiert:
  - Mitarbeitendenverwaltung fuer Stammdaten und aktuellen Vertragsstand
  - EF Core / SQLite Persistenz
  - Avalonia UI mit Liste, Suche, Filter, Ansichts-/Bearbeitungsmodus und Archivierungsfluss
- Payroll-Domain bildet bereits Grundlohn, Zuschlagslinien, Spesen, Fahrzeugentschaedigung und fixen BVG-Abzug als auditierbare Ableitung ab
- `EmployeeMonthlyRecord` als gemeinsamer Monatskontext fuer Zeiten und Spesen ist umgesetzt
- erster funktionaler UI-/Service-Schnitt fuer manuelle Monatserfassung ist vorhanden und im Save-Flow stabilisiert
- eigener Bereich `Zeit- und Spesenerfassung` ist umgesetzt:
  - Monat zuerst
  - Mitarbeitendenliste im selben Arbeitsbereich
  - Erfassung rechts
  - Personendaten separat
- Jahresdarstellung und Lohnblatt sollen spaeter aus Monatsdaten abgeleitet werden
- Domain-, Repository-, Application- und ViewModel-Tests sind vorhanden
- `dotnet build` funktioniert stabil; voller `dotnet test`-Lauf wurde ausserhalb der Sandbox erfolgreich verifiziert

---

# Aktuelle Themen

- `EmployeeMonthlyRecord` weiter ausbauen:
  - Vertragshistorie sauber an den Monatskontext anschliessen
  - Statusfluss fuer Review und Payroll-Uebernahme schaerfen
  - Fahrzeugentschaedigung als eigener editierbarer Unterbereich
- Monatserfassung UX-seitig weiterziehen:
  - von stabiler Einzelperson-Erfassung zu echter Mehrpersonen-Monatstabelle
  - excel-artige Eingabe fuer mehrere Mitarbeitende im selben Monat
- Vertragshistorie fuer `EmploymentContract` fachlich konkretisieren:
  - aktueller Vertragsstand vs. mehrere historisierte Versionen
  - klare Gueltigkeits- und Uebergangsregeln
- naechsten kleinen Payroll-Orchestrierungsschritt in der Application-Schicht vorbereiten
- manuelle Erfassung kommt vor Import; spaetere Importpfade muessen denselben normalisierten Zielzustand befuellen

---

# Nächster konkreter Schritt

- die jetzige Einzelperson-Erfassung in eine echte monatszentrierte Mehrpersonen-Erfassung ueberfuehren

---

# Offene fachliche Punkte

- Überlappung von Nacht-/Sonntags-/Feiertagsstunden
- genaue Definition der Zuschlagslogik
- BVG-Pro-Rata bei Teilmonaten
- Umgang mit Spezialfällen:
  - Fahrzeitentschädigung
  - Mehrzeit / Unterzeit
  - Weiterbildung
  - Unfalltaggeld

---

# Risiken

- falsche Annahmen bei Zuschlagslogik koennten spaetere Umbauten erzwingen
- unklare Vertragsstruktur ohne explizite Historisierung kann spaetere Daten- und Prozessumbrueche verursachen
- Excel enthaelt implizite Regeln und Spezialfaelle, die noch nicht vollstaendig extrahiert sind
- Detailregeln fuer Status, Monatsabschluss und Uebernahme in den Lohnlauf sind noch nicht fachlich entschieden
- die aktuelle Monatserfassung ist zwar jetzt funktional, aber noch nicht die vom Fachbild gewuenschte tabellarische Monatsansicht ueber mehrere Mitarbeitende

---

# Letzte wichtige Entscheidungen

- Zuschlagsarten (Nacht/Sonntag/Feiertag) sind aktuell Modellannahme, nicht Excel-gesichert
- unklare Regeln werden nicht implementiert, sondern explizit markiert
- Fokus auf stabile Domain vor komplexer Payroll-Logik
- Fahrzeugentschädigung wird zentral modelliert

---

# Hinweise für AI / Codex

- Excel ist fachliche Referenz, nicht technische Vorlage
- keine hardcodierten Sätze oder Regeln
- Regeln müssen konfigurierbar bleiben
- keine Businesslogik in UI
- Änderungen nur modular und ohne Seiteneffekte

---

# Hinweise für Mentoring / Review

- Fokus auf saubere Domain-Modellierung
- Priorität: Stabilität vor Feature-Vollständigkeit
- zuerst Stammdaten und Zeitdaten, danach Payroll vertiefen
