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
- die Monatserfassung kann fuer den aktuell gewaehlten Mitarbeitenden eine Lohn-Voransicht aus Vertrag, zentralen Settings sowie Zeit- und Spesendaten ableiten
- `EmployeeMonthlyRecord` ist der explizite Monatsanker fuer:
  - genau einen Mitarbeitenden
  - genau einen Abrechnungsmonat
  - Status der Erfassung
  - einen Snapshot der fuer diesen Monat verwendeten lohnrelevanten Berechnungsparameter
- referentielle Regeln:
  - genau ein Monatskontext pro Mitarbeitenden und Monat
  - Zeit- und Spesenwerte verweisen referenziell auf genau einen Monatskontext

## Bewegungsdaten

- `TimeEntry`
  - Datum
  - Arbeitsstunden
  - Nachtstunden
  - Sonntagsstunden
  - Feiertagsstunden
  - `Fahrzeugentschaedigung Pauschalzone 1` CHF
  - `Fahrzeugentschaedigung Pauschalzone 2` CHF
  - `Fahrzeugentschaedigung Regiezone 1` CHF
  - optionale Bemerkung
- `ExpenseEntry`
  - genau ein Datensatz pro Mitarbeitenden und Monat
  - `Spesen` Total CHF

## Zentrale Settings

- `PayrollSettings`
  - zentrale mehrzeilige `Firmenadresse` fuer PDF-/Lohnblatt-Kopf
  - Bereich `Berechnung` ist strukturell in `Allgemein`, `Stundenlohn` und vorbereitend `Monatslohn` gegliedert
  - zentrale Zuschlagssaetze fuer Nacht, Sonntag und Feiertag
  - zentrale pflegbare Listen fuer:
    - `Abteilung`
    - `Anstellungskategorie`
    - `Anstellungsort`
  - zentrale prozentuale Abzugsparameter fuer:
    - `AHV/IV/EO`
    - `ALV`
    - `Krankentaggeld/UVG`
    - `Aus- und Weiterbildung inkl. Ferien`
  - zentrale `FerienentschaedigungRate`
  - zentrale `FerienentschaedigungRateAb50`
  - zentrale CHF-Ansatzwerte fuer:
    - `Pauschalzone 1`
    - `Pauschalzone 2`
    - `Regiezone 1`
- Payroll-Regel:
  - pro `EmployeeMonthlyRecord` wird beim erstmaligen Arbeiten im Monat ein Snapshot der lohnrelevanten Berechnungsparameter gespeichert; spaetere Aenderungen in `PayrollSettings` wirken nicht rueckwirkend auf bereits erfasste Monate
  - Fahrzeugentschaedigung in der Lohnberechnung = erfasste Menge aus den Zeitdaten * zentraler CHF-Ansatz aus `PayrollSettings`
  - `Ferienentschaedigung` = altersabhaengiger Satz aus `PayrollSettings` * (Basislohn + Zeitzuschlaege + Spezialzuschlag + Fahrzeitentschaedigung)
  - Alter wird aus dem Geburtsdatum des Mitarbeitenden auf Basis des Abrechnungsmonats bestimmt; ab 50 Jahren gilt `FerienentschaedigungRateAb50`, sonst `FerienentschaedigungRate`
  - prozentuale Abzuege werden auf den lohnrelevanten Bruttobetrag angewendet
  - `Total Auszahlung` der Voransicht wird analog Excel-Vorlage auf 5 Rappen gerundet
  - die Seite `Lohnlaeufe` kann aus Monatsdaten, Mitarbeitendenstammdaten und `PayrollSettings` ein PDF-Lohnblatt ableiten; der PDF-Kopf verwendet die zentrale Firmenadresse aus den Settings

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
  - `Lohnart`
    - `Stundenlohn`
    - `Monatslohn`
  - referenzielle Zuordnung zu:
    - `Abteilung`
    - `Anstellungskategorie`
    - `Anstellungsort`
  - Wohnsitzland
  - Nationalitaet
  - Bewilligung
  - Steuerstatus
  - Quellensteuerpflicht
  - AHV-Nummer
  - IBAN
- `EmploymentContract`
  - Stundenlohn CHF
  - BVG-Abzug pro Monat CHF
  - `Spezialzuschlag gemaess Vertrag` CHF pro Arbeitsstunde

## Versionierung

- Beitragssätze
- Steuerregeln
- Versicherungsregeln
- zentrale Zuschlagssaetze fuer Nacht, Sonntag und Feiertag

## Audit

- jede Änderung protokollieren
