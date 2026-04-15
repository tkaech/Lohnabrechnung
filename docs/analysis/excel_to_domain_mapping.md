# Excel → Domain Mapping

## Std-Erf
→ PayrollRun / Aggregation

## Mitarbeiter-Blätter
→ TimeEntry, ExpenseEntry, PayrollRunLine

## Arbeitsstunden
→ TimeEntry (Arbeitsstunden)
→ PayrollRunLine (Grundlohn)

## Zuschläge
→ TimeEntry (Nacht-, Sonntags- und Feiertagsstunden)
→ Payroll-Regeln fuer Bewertung im Lohnlauf

## Spesen
→ ExpenseEntry
→ mit fachlicher Spesenart

## BVG
→ EmploymentContract (fixer Monatsbetrag)
→ PayrollRunLine (fixe Abzugszeile im Lohnlauf)

## Fahrzeug
→ VehicleCompensation 

## Quellensteuer
→ TaxRule (Tariftabelle)

## Secplan Export
→ Infrastructure Import Mapping
→ TimeEntry (normalisierte Zeitdaten)
