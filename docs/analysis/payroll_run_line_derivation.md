# PayrollRunLine Ableitungslogik

## Fachliche Definition

`PayrollRunLine` ist in diesem Projekt die kleinste fachliche, auditierbare Ergebniszeile eines Lohnlaufs fuer genau eine Person und genau einen Lohnbestandteil.

Eine `PayrollRunLine` ist nicht der Rohimport und auch nicht der Arbeitsvertrag selbst. Sie ist das verdichtete Resultat eines gesicherten Payroll-Bausteins wie:
- Grundlohn aus Arbeitsstunden
- Zuschlagslohn fuer Nacht-, Sonntags- oder Feiertagsstunden
- Spesenerstattung
- Fahrzeugentschaedigung
- fixer BVG-Abzug

## Eingabedaten

### Direkt uebernommene Werte
- Spesenbetraege in CHF aus `ExpenseEntry`
- Fahrzeugentschaedigung in CHF aus `VehicleCompensation`
- Arbeitsstundenmengen aus `TimeEntry`
- Nachtstundenmengen aus `TimeEntry`
- Sonntagsstundenmengen aus `TimeEntry`
- Feiertagsstundenmengen aus `TimeEntry`
- Stundenlohn aus `EmploymentContract`
- fixer BVG-Betrag aus `EmploymentContract`
- konfigurierbare Zuschlagsparameter aus `EmploymentContract`

### Berechnete Werte
- Grundlohn = Arbeitsstunden * Stundenlohn
- Nachtzuschlag = Nachtstunden * Stundenlohn * Nachtzuschlagsrate
- Sonntagszuschlag = Sonntagsstunden * Stundenlohn * Sonntagszuschlagsrate
- Feiertagszuschlag = Feiertagsstunden * Stundenlohn * Feiertagszuschlagsrate
- BVG-Abzug = fixer Vertragswert als negative Payroll-Zeile

### Noch Offene Oder Unklare Regeln
- Ob Nacht-, Sonntags- und Feiertagsstunden sich fachlich gegenseitig ausschliessen oder kumulierbar sind
- Ob und wie fixer BVG bei Ein- und Austritten innerhalb eines Monats pro rata behandelt wird
- Welche Spesenarten fachlich als eigene Linien aggregiert oder nur klassifiziert werden sollen
- Welche weiteren Zuschlagsarten ausser Nacht, Sonntag und Feiertag aus dem Excel noch benoetigt werden
- Wie Sozialversicherungen und Quellensteuer spaeter exakt aus den Lohnlinien abgeleitet werden

## Ableitungsregeln

### Grundlohn
- Wird aus der Summe der Arbeitsstunden eines Abrechnungszeitraums abgeleitet
- Verwendet den gueltigen Stundenlohn des Vertrags
- Ergibt eine berechnete `PayrollRunLine`

### Zuschlagslinien
- Nacht, Sonntag und Feiertag sind eigene Ergebnislinien
- Die Menge kommt direkt aus den erfassten Spezialstunden
- Die Bewertung erfolgt nur dann, wenn die passende Zuschlagsrate im Vertrag konfiguriert ist
- Fehlt eine Rate, entsteht keine unsichere Berechnung; stattdessen bleibt dies ein offener Punkt der Ableitung

### Spesen
- Spesen werden betraglich direkt in CHF uebernommen
- Eine fachliche Spesenart dient zur Klassifikation, nicht zur automatischen Bewertung
- Jede Spese kann als direkte Payroll-Zeile erscheinen

### Fahrzeugentschaedigung
- Wird getrennt von normalen Spesen behandelt
- Betrag wird direkt uebernommen

### BVG
- Wird als fixe negative Payroll-Zeile aus dem Vertrag abgeleitet
- Grundlage ist der gueltige Vertrag zum Payroll-Referenzdatum
- Eine moegliche Pro-Rata-Regel fuer Teilmonate ist aktuell offen und wird nicht stillschweigend angenommen

## Modellierungsprinzipien

- `PayrollRunLine` repraesentiert ein Ergebnis, nicht die gesamte Regel
- Rohdaten bleiben in `TimeEntry`, `ExpenseEntry`, `VehicleCompensation` und `EmploymentContract`
- Die Ableitung erfolgt ueber einen kleinen fachlichen Service
- Offene oder fehlende Regeln werden als Issues zurueckgegeben und nicht hardcodiert
