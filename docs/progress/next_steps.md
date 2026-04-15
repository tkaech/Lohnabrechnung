# Next Steps

1. die neue Unterseite `Monats-/Stundenerfassungen` im naechsten Schritt um direkte Spruenge in die Einzel-Erfassung pro Mitarbeitendem erweitern, damit Monatsliste und Detailbearbeitung enger zusammenarbeiten
2. nach dem ersten Windows-Testlauf entscheiden, ob der aktuelle portable `win-x64`-Publish als ZIP ausreicht oder als naechster Schritt ein echter Installer vorbereitet werden soll
3. den neuen Backup-/Restore-Bereich um Dateiauswahl-Dialoge und eine kleine Sicherungs-Vorschau erweitern, damit Pfadwahl und Sicherungsart noch gefuehrter ablaufen
4. die neue zentrale Theme-Struktur schrittweise in weitere Views und Panels uebernehmen, damit noch mehr hart codierte Farben und Typografie aus den Views verschwinden
5. das jetzt in `Settings` pflegbare Druck-Template um Validierung oder kleine Template-Hinweise erweitern, damit Platzhalterfehler frueher sichtbar werden
6. die getrennte Monatslogik `reine Spesen` plus `lohnrelevante Fahrzeugwerte in der Zeitzeile` im kuenftigen Mehrpersonen-Layout konsistent mitfuehren, ohne wieder Listen oder Detailsektionen einzufuehren
7. Vertragshistorie als historisierte `EmploymentContract`-Versionen mit klaren Gueltigkeitsregeln konkretisieren und sauber an den Monatskontext anschliessen
8. Naechsten kleinen Payroll-Orchestrierungsschritt in der Application-Schicht definieren: gueltigen Vertrag, zentrale Zuschlags-, Abzugs-, altersabhaengige Ferienentschaedigungs- und Fahrzeugsettings, WorkSummary und Spesen aus `EmployeeMonthlyRecord` zusammenfuehren, ohne offene Spezialfaelle zu automatisieren
9. fuer bestehende Altmonate ohne Snapshot einen kleinen Hinweis oder gefuehrten Aktualisierungspfad vorsehen, damit klar bleibt, ab wann Monatsparameter historisch eingefroren sind
10. auf Basis der neuen `Lohnart` im Mitarbeitendenstamm den ersten fachlich klar abgegrenzten Monatslohn-Schnitt definieren, z. B. separate Vertragsparameter und eine bewusste Payroll-Behandlung fuer `Monatslohn` statt stillschweigender Stundenlohn-Ableitung
11. die neue Herleitungsansicht im Bereich `Lohnlaeufe` um gezielte Interaktion erweitern, z. B. Klick/Hover auf gemeinsame Kennungen, damit zusammengehoerige Eingaben, Rechenschritte und Ergebniszeilen direkt gemeinsam hervorgehoben werden
12. den neuen Stundenimport nach dem ersten funktionalen Schnitt um CSV-Vorschau, differenziertere Konfliktmeldungen und feinere Ersetzungsstrategien pro Monat erweitern
13. den Personendaten-Import um manuelle Vorschau, Konfliktanzeige und optionales Validierungsprotokoll pro Zeile erweitern
14. als naechsten Payroll-Schritt die Monats-/Snapshot-Aufloesung von den neuen getrennten globalen Bereichsstaenden `Allgemein`, `Stundenlohn` und `Monatslohn` lesen, statt nur die aktuelle Legacy-Projektion in `PayrollSettings` zu verwenden
