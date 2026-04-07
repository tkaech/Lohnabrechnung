Payroll Desktop - Windows-Test
==============================

Start
-----
1. `Payroll.Desktop.exe` starten
2. Beim ersten Start wird automatisch eine SQLite-Datenbank angelegt
3. Es werden nur Konfigurationsdaten vorbereitet
4. Es werden keine Demo-Mitarbeitenden angelegt


Wo liegen die Daten?
--------------------
Standardmaessig hier:
`C:\Users\<Benutzer>\AppData\Local\PayrollApp\payroll.test.db`


Wichtig
-------
- Beim zweiten Start wird dieselbe Datenbank weiterverwendet
- Bestehende Daten werden nicht ueberschrieben
- Ausfuehrliche Hinweise stehen in:
  `WINDOWS-TEST-STARTANLEITUNG.txt`


Optionaler Start mit anderem DB-Pfad
------------------------------------
`Payroll.Desktop.exe --db-path="C:\Pfad\zur\payroll.test.db"`
