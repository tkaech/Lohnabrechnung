# Datenmodell

## Entitäten

- Employee
- EmploymentContract
- EmployeeMonthlyRecord
- PayrollSettings
- PayrollPeriod
- PayrollRun
- PayrollRunLine
- PayItemType
- TimeEntry
- ExpenseEntry
- ManualAdjustment
- ContributionRate
- InsuranceRule
- TaxRule
- AuditLog

## Prinzipien

- Trennung:
  - Stammdaten
  - Bewegungsdaten
  - Regeln
  - Ergebnisse
- fuehrender Erfassungskontext fuer Zeiten und Spesen:
  - Mitarbeitender
  - Abrechnungsmonat

## Monatserfassung

- Zeiten und Spesen werden fachlich im selben Monatskontext gefuehrt
- Jahresdarstellungen und Lohnblaetter werden spaeter aus Monatsdaten abgeleitet
- Monatsvorschau ist eine Verdichtung und keine zusaetzliche Primärerfassung
- `EmployeeMonthlyRecord` ist der explizite Monatsanker fuer:
  - genau einen Mitarbeitenden
  - genau einen Abrechnungsmonat
  - Status der Erfassung
- referentielle Regeln:
  - genau ein Monatskontext pro Mitarbeitenden und Monat
  - Zeit- und Speseneintraege verweisen referenziell auf genau einen Monatskontext
  - Fahrzeugentschaedigung bleibt fachlich getrennt, kann aber am selben Monatskontext haengen

## Bewegungsdaten

- `TimeEntry`
  - Datum
  - Arbeitsstunden
  - Nachtstunden
  - Sonntagsstunden
  - Feiertagsstunden
  - optionale Bemerkung
- `ExpenseEntry`
  - Datum
  - Betrag CHF
  - fachlich genau ein Monatstotal `Diverse Spesen` je Monatskontext
- Fahrzeugentschaedigung bleibt fachlich getrennt von normalen Spesen

## Employee Stammdaten

- Kernidentität:
  - Personalnummer
  - Vorname
  - Nachname
  - Geburtsdatum
  - Eintrittsdatum
  - Austrittsdatum
  - aktiv/inaktiv
- Kontakt:
  - Telefon
  - E-Mail
- strukturierte Adresse:
  - Strasse
  - Hausnummer
  - Adresszusatz
  - Postleitzahl
  - Ort
  - Land
- vorbereitete payroll-relevante Stammdaten:
  - Wohnsitzland
  - Nationalitaet
  - Bewilligung
  - Steuerstatus
  - Quellensteuerpflicht
  - AHV-Nummer
  - IBAN

## Versionierung

- Beitragssätze
- Steuerregeln
- Versicherungsregeln
- zentrale Zuschlagssaetze fuer Nacht, Sonntag und Feiertag

## Audit

- jede Änderung protokollieren
