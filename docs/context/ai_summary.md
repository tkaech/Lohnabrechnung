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
- gemeinsamer Monatskontext fuer Zeiten und Spesen ist als naechstes fachliches Sollbild dokumentiert
- Jahresdarstellung und Lohnblatt sollen spaeter aus Monatsdaten abgeleitet werden
- Domain-, Repository-, Application- und ViewModel-Tests sind vorhanden
- `dotnet build` funktioniert stabil; `dotnet test` ist in dieser Sandbox weiterhin technisch limitiert

---

# Aktuelle Themen

- gemeinsame `Monatserfassung pro Mitarbeitenden` fachlich weiter zuschneiden:
  - Mitarbeitender
  - Abrechnungsmonat
  - gemeinsame Arbeitsflaeche fuer Zeiten und Spesen
  - Monatsvorschau als Verdichtung
- Vertragshistorie fuer `EmploymentContract` fachlich konkretisieren:
  - aktueller Vertragsstand vs. mehrere historisierte Versionen
  - klare Gueltigkeits- und Uebergangsregeln
- naechsten kleinen Payroll-Orchestrierungsschritt in der Application-Schicht vorbereiten
- manuelle Erfassung kommt vor Import; spaetere Importpfade muessen denselben normalisierten Zielzustand befuellen

---

# Nächster konkreter Schritt

- Vertragshistorie als historisierte `EmploymentContract`-Versionen mit klaren Gueltigkeitsregeln definieren

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
- fuer die gemeinsame Monatserfassung ist noch offen, ob der Monatskontext spaeter eine eigene explizite Entitaet benoetigt
- Detailregeln fuer Status, Monatsabschluss und Uebernahme in den Lohnlauf sind noch nicht fachlich entschieden
- `dotnet test` ist in dieser Sandbox technisch nicht verlässlich lokal ausfuehrbar

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
