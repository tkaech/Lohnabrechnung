Payroll Desktop - Windows-Test
==============================

Start
-----
1. `Payroll.Desktop.exe` starten
2. Beim ersten Start wird automatisch eine SQLite-Datenbank angelegt
3. Das Schema wird per Migrationen aufgebaut
4. Fuer diesen Windows-Test werden Testdaten angelegt


Wo liegen die Daten?
--------------------
Standardmaessig hier:
`C:\Users\<Benutzer>\AppData\Local\PayrollApp\payroll.windows-test.db`


Wichtig
-------
- Beim zweiten Start wird dieselbe Datenbank weiterverwendet
- Bestehende Daten werden nicht ueberschrieben
- Ausfuehrliche Hinweise stehen in:
  `WINDOWS-TEST-STARTANLEITUNG.txt`


Wenn der Start fehlschlaegt
--------------------------
1. Anwendung schliessen
2. diese Datei loeschen, falls sie schon existiert:
   `C:\Users\<Benutzer>\AppData\Local\PayrollApp\payroll.windows-test.db`
3. Anwendung erneut starten

Hinweis:
- Beim naechsten Start wird die Test-Datenbank automatisch neu aufgebaut
- Falls weiterhin ein Fehler erscheint, die Datei
  `C:\Users\<Benutzer>\AppData\Local\PayrollApp\startup-error.log`
  pruefen


Optionaler Start mit anderem DB-Pfad
------------------------------------
`Payroll.Desktop.exe --db-path="C:\Pfad\zur\payroll.windows-test.db"`
