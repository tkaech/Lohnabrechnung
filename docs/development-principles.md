# Development Principles

Diese Regeln gelten projektweit und sollen bei jeder neuen Funktion, jeder Refaktorierung und jeder UI-Erweiterung beachtet werden.

## DRY

- Fachliche Logik wird nur einmal implementiert.
- Gleiche oder sehr aehnliche Logik wird nicht mehrfach per Copy/Paste erstellt.
- Wiederverwendbare Logik gehoert in zentrale Services, gemeinsame Komponenten oder klar definierte Hilfsklassen.

## Single Source of Truth

- Fachregeln, Konfigurationen und Berechnungsgrundlagen haben jeweils genau eine zentrale Quelle.
- Werte wie Abzugssaetze, Statusdefinitionen, Formatierungsregeln und Validierungslogik duerfen nicht mehrfach im Code verteilt sein.
- Wenn bestehende Logik erweitert werden kann, wird sie bevorzugt erweitert statt parallel neu aufgebaut.

## Consistent UI Behavior

- Gleiche Daten muessen in der gesamten Anwendung gleich dargestellt werden.
- Gleiche Benutzeraktionen muessen sich in allen Ansichten gleich verhalten.
- Datums-, Waehrungs-, Status- und Fehlermeldungsdarstellungen werden zentral definiert und wiederverwendet.

## No Business Logic in UI

- Keine Businesslogik in Views, Code-Behind oder UI-spezifischen Komponenten.
- Die UI spricht nur mit ViewModels, Application-Services und klaren Datenmodellen.
- Berechnungen und fachliche Entscheidungen gehoeren in Domain oder Application.

## Reuse Before Creation

- Vor jeder neuen Klasse, View, Methode oder Komponente ist zu pruefen, ob bereits etwas Wiederverwendbares existiert.
- Bestehende Strukturen werden bevorzugt erweitert, bevor neue parallele Strukturen eingefuehrt werden.
- Neue Abstraktionen sollen nur dann eingefuehrt werden, wenn sie echte Wiederverwendung oder Klarheit schaffen.

## Practical Consequences

- Gemeinsame Darstellungslogik wird in wiederverwendbaren UI-Komponenten oder zentralen Formatierern gekapselt.
- Fachliche Berechnungen werden in dedizierten Services oder Domain-Klassen gebuendelt.
- Wiederkehrende Regeln werden getestet, damit sie nicht unbemerkt an mehreren Stellen auseinanderlaufen.
