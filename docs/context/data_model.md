# Datenmodell

## Entitäten

- Employee
- EmploymentContract
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
- offen:
  - ob `MonthlyEntryContext` spaeter als eigene Entitaet benoetigt wird oder zunaechst implizit ueber Mitarbeitendenbezug plus Monat gefuehrt wird, ist noch nicht entschieden

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
  - Spesenart
  - Betrag CHF
  - optionale Beschreibung oder Referenz
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

## Audit

- jede Änderung protokollieren
