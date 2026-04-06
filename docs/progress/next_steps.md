# Next Steps

1. Monatserfassung von der jetzt funktionsfaehigen Einzelperson-Erfassung in eine echte monatszentrierte, tabellarische Erfassung fuer mehrere Mitarbeitende weiterentwickeln
2. die getrennte Monatslogik `reine Spesen` plus `lohnrelevante Fahrzeugwerte in der Zeitzeile` im kuenftigen Mehrpersonen-Layout konsistent mitfuehren, ohne wieder Listen oder Detailsektionen einzufuehren
3. Vertragshistorie als historisierte `EmploymentContract`-Versionen mit klaren Gueltigkeitsregeln konkretisieren und sauber an den Monatskontext anschliessen
4. Naechsten kleinen Payroll-Orchestrierungsschritt in der Application-Schicht definieren: gueltigen Vertrag, zentrale Zuschlags-, Abzugs-, Ferienentschaedigungs- und Fahrzeugsettings, WorkSummary und Spesen aus `EmployeeMonthlyRecord` zusammenfuehren, ohne offene Spezialfaelle zu automatisieren
