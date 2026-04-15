# Fachliche Entscheidungen Aus Offenen Fragen

## Zuschläge

### Entscheidung
- Zuschläge werden weiterhin als Stunden erfasst, nicht als Prozent oder CHF-Betrag
- Die prozentuale oder tarifliche Bewertung von Zuschlagsstunden erfolgt erst im Payroll-Modul
- Die Bewertungslogik wird regelbasiert und versionierbar modelliert, nicht in `TimeEntry`

### Auswirkung Auf Das Domain-Modell
- `TimeEntry` bleibt für Mengen zuständig: Basisstunden und Zuschlagsstunden
- Die spätere Lohnbewertung braucht eigene Payroll-Regeln je Zuschlagsart oder Zuschlagscode
- Prozentwerte sind fachliche Payroll-Regeln, nicht Teil der Zeiterfassung

## Spesenarten

### Entscheidung
- Es gibt mehrere fachliche Spesenarten
- Alle Spesenarten werden in CHF geführt
- Fahrzeugentschädigung bleibt eine eigene zentrale Entität und ist keine normale Spesenart

### Auswirkung Auf Das Domain-Modell
- `ExpenseEntry` repräsentiert allgemeine Spesen in CHF
- Fachlich ist eine Klassifikation nach Spesenart vorgesehen, auch wenn diese im aktuellen Code noch nicht separat modelliert ist
- Fahrzeugentschädigung wird weiterhin getrennt über `VehicleCompensation` behandelt

## BVG

### Entscheidung
- BVG wird pro Mitarbeitendem als fixer CHF-Betrag pro Abrechnungsmonat festgelegt
- Die Quelle für diesen Betrag ist der zum Zeitraum gültige Arbeitsvertrag
- BVG ist keine prozentuale Berechnung aus den erfassten Stunden

### Auswirkung Auf Das Domain-Modell
- Der fixe BVG-Betrag gehört fachlich zum `EmploymentContract`
- Im Lohnlauf wird daraus eine feste Abzugszeile in `PayrollRunLine`
- Vertragswechsel müssen über Gültigkeitszeiträume abbildbar sein

## Quellensteuer

### Entscheidung
- Es gibt mehrere Quellensteuertarife
- Die Auswahl erfolgt über Kanton, Tarifcode und zeitliche Gültigkeit
- Die konkrete Zuordnung eines Tarifs zu einem Mitarbeitenden gehört in die Stammdaten oder in spätere Payroll-Orchestrierung, nicht in die Tarifdefinition selbst

### Auswirkung Auf Das Domain-Modell
- `TaxRule` bleibt versionierbar und tariffähig
- Mehrere parallele Tarife sind fachlich vorgesehen und kein Sonderfall
- Mitarbeiterbezogene Steuermerkmale werden getrennt von der Tariftabelle behandelt

## Secplan Exportformat

### Entscheidung
- Das konkrete Secplan-Dateiformat ist kein Domain-Entscheid, sondern eine Integrationsfrage der Infrastructure
- Domain-seitig wird nur ein normalisiertes Ergebnis benötigt: Arbeitsdatum, Mitarbeitendenbezug, Stunden und gegebenenfalls Zuschlagsstunden
- Der Import muss formatabhängig konfigurierbar sein und darf nicht auf ein einziges fest codiertes Exportlayout zugeschnitten werden

### Auswirkung Auf Das Domain-Modell
- Das Domain-Modell bleibt unabhängig vom Rohformat von Secplan
- Importprofile oder Mapping-Regeln werden später in der Infrastructure behandelt
- Die offene technische Klärung des Dateiformats bleibt bestehen, blockiert aber die Domain-Modellierung nicht mehr
