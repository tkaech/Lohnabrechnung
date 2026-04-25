Du arbeitest an einer Einzelplatz-Lohnsoftware in C# mit Clean Architecture, Avalonia UI und SQLite.

Beachte als führende Projektregeln nur diese Dokumente:

* docs/context/architecture.md
* docs/context/module_boundaries.md
* docs/context/coding_rules.md
* docs/context/data_model.md
* docs/progress/status.md

Zusätzliche Orientierung nur bei Bedarf:

* docs/progress/next_steps.md
* docs/progress/session_handover.md
* docs/planning/decisions.md
* docs/planning/pendenzen.md

Verbindliche Arbeitsregeln:

* Domain, Application, Infrastructure und UI strikt trennen
* keine Businesslogik in UI
* bestehende Muster wiederverwenden statt neue Patterns einzuführen
* nur betroffene Dateien laden und ändern
* keine Scope-Erweiterung
* Historisierung strikt umsetzen, wenn ein Bereich versioniert ist:

  * ValidFrom/ValidTo
  * keine Überschneidungen
  * aktueller Stand = Update
  * neuer Stand = Insert
  * keine impliziten Überschreibungen

Dokumentation nur bei relevanten Änderungen nachführen:

* docs/context/data_model.md bei echten Struktur- oder Modelländerungen
* docs/progress/status.md bei relevantem umgesetztem Fortschritt

Nicht nachführen:

* keine Detailprotokolle
* keine rein kosmetischen Änderungen
* keine Doku-Anpassung ohne fachliche Relevanz

Antwortstil:

* präzise
* technisch
* minimal
* keine langen Erklärungen
* zuerst Ist-Analyse, dann Modellentscheidung, dann Umsetzung

Arbeite nur auf Basis der von mir jeweils genannten Dateien und Aufgaben.

Aktuelle Aufgabe:
[HIER DIE KONKRETE AUFGABE]

