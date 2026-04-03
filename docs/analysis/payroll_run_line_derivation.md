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

### Gesicherte Erkenntnisse Aus Excel Und Bestehendem Modell
- In der Sammelansicht `Std-Erf` gibt es die Spalte `ZUSCHLÄGE NSF`
- Im individuellen Lohnblatt gibt es die Lohnzeile `Spezialzuschlag gemäss Vertrag`
- Im individuellen Lohnblatt gibt es die Lohnzeile `BVG Arbeitnehmeranteil`
- Im individuellen Lohnblatt gibt es weitere separate Spezialpositionen wie `Fahrzeitentschädigung`, `Mehrzeit/Unterzeit`, `Aus- und Weiterbildungskosten inkl. Ferienentschädigung` und in Spezialblättern `Unfalltaggeld`
- Daraus ist fachlich gesichert, dass nicht jede Sonderposition ein normaler Zuschlag ist; ein Teil sind eigene Payroll-Bestandteile

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
- Welche weiteren Spezialpositionen aus dem Excel spaeter als eigene Payroll-Line-Typen modelliert werden sollen
- Wie Sozialversicherungen und Quellensteuer spaeter exakt aus den Lohnlinien abgeleitet werden

### Fachliche Annahmen Im Aktuellen Modell
- Nacht, Sonntag und Feiertag werden als getrennte Eingabemengen gefuehrt, obwohl Excel dafuer nur den Sammelbegriff `NSF` sicher belegt
- Der fixe BVG-Betrag wird aktuell vollstaendig verwendet, wenn der Vertrag am Payroll-Referenzdatum gueltig ist
- Weitere Spezialpositionen aus dem Excel werden derzeit noch nicht automatisch abgeleitet

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
- Uebersteigt die Summe der Spezialstunden die Gesamtarbeitsstunden, wird keine Zuschlagsberechnung vorgenommen; stattdessen wird ein fachlicher Konflikt gemeldet, weil die Ueberlappungsregel offen ist

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
- Die aktuelle Ableitung trifft deshalb bewusst keine Teilmonatsberechnung

## Modellierungsprinzipien

- `PayrollRunLine` repraesentiert ein Ergebnis, nicht die gesamte Regel
- Rohdaten bleiben in `TimeEntry`, `ExpenseEntry`, `VehicleCompensation` und `EmploymentContract`
- Die Ableitung erfolgt ueber einen kleinen fachlichen Service
- Offene oder fehlende Regeln werden als Issues zurueckgegeben und nicht hardcodiert
