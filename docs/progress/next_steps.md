# Next Steps

1. Monatserfassung von der jetzt funktionsfaehigen Einzelperson-Erfassung in eine echte monatszentrierte, tabellarische Erfassung fuer mehrere Mitarbeitende weiterentwickeln
2. fuer den ersten Windows-Testlauf eine kurze Betriebsdokumentation mit konkreten Startbeispielen fuer `appsettings`, ENV-Override und `--db-path`/`--environment` nachziehen
3. den neuen Backup-/Restore-Bereich um Dateiauswahl-Dialoge und eine kleine Sicherungs-Vorschau erweitern, damit Pfadwahl und Sicherungsart noch gefuehrter ablaufen
4. die neue zentrale Theme-Struktur schrittweise in weitere Views und Panels uebernehmen, damit noch mehr hart codierte Farben und Typografie aus den Views verschwinden
5. das jetzt in `Settings` pflegbare Druck-Template um Validierung oder kleine Template-Hinweise erweitern, damit Platzhalterfehler frueher sichtbar werden
6. die getrennte Monatslogik `reine Spesen` plus `lohnrelevante Fahrzeugwerte in der Zeitzeile` im kuenftigen Mehrpersonen-Layout konsistent mitfuehren, ohne wieder Listen oder Detailsektionen einzufuehren
7. Vertragshistorie als historisierte `EmploymentContract`-Versionen mit klaren Gueltigkeitsregeln konkretisieren und sauber an den Monatskontext anschliessen
8. Naechsten kleinen Payroll-Orchestrierungsschritt in der Application-Schicht definieren: gueltigen Vertrag, zentrale Zuschlags-, Abzugs-, altersabhaengige Ferienentschaedigungs- und Fahrzeugsettings, WorkSummary und Spesen aus `EmployeeMonthlyRecord` zusammenfuehren, ohne offene Spezialfaelle zu automatisieren
9. fuer bestehende Altmonate ohne Snapshot einen kleinen Hinweis oder gefuehrten Aktualisierungspfad vorsehen, damit klar bleibt, ab wann Monatsparameter historisch eingefroren sind
