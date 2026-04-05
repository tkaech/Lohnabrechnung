# Domain Decisions

## Stunden
- werden immer als decimal gespeichert
- manuelle Erfassung und spaetere Importpfade muessen dieselben normalisierten Zeitdaten befuellen

## Monatserfassung
- fuehrender fachlicher Kontext fuer Zeiten und Spesen ist:
  - Mitarbeitender
  - Abrechnungsmonat
- Zeiten und Spesen werden fachlich in derselben Arbeitsflaeche gefuehrt
- Jahresdarstellungen und Lohnblaetter werden spaeter aus Monatsdaten abgeleitet
- eine Monatsvorschau ist fachliche Verdichtung, keine zusaetzliche Rohdatenerfassung
- `EmployeeMonthlyRecord` ist als eigene explizite Monatsanker-Entitaet umgesetzt
- diese Entitaet sichert:
  - eindeutigen Monatskontext
  - referentielle Integritaet fuer Zeit- und Spesendaten
  - spaetere Erweiterbarkeit fuer Import, Review, Monatsabschluss und Payroll-Uebernahme

## Zuschläge
- werden als Stunden gespeichert
- Berechnung erfolgt im Payroll-Modul
- Prozentwerte oder Bewertungsfaktoren gehören zu versionierbaren Payroll-Regeln, nicht zu `TimeEntry`
- Nacht-, Sonntags- und Feiertagsstunden werden als eigene fachliche Mengen behandelt
- fehlende Zuschlagsregeln werden nicht implizit berechnet
- bei unklarer Ueberlappung der Spezialstunden wird keine additive Berechnung hardcodiert; stattdessen entsteht ein fachlicher Hinweis

## Spesen
- immer CHF
- mehrere Spesenarten sind fachlich vorgesehen
- Fahrzeugentschädigung ist davon getrennt
- monatliche manuelle Erfassung erfolgt im selben Kontext wie Zeiten

## Fahrzeugentschädigung
- zentral erfassen
- nicht pro Formular
- fachlich getrennt von normalen Spesen, aber an denselben Monatskontext anbindbar

## BVG
- fixer CHF-Betrag
- nicht prozentual
- pro Mitarbeitendem und gültigem Arbeitsvertrag festgelegt
- wird im Lohnlauf als fixe Abzugsposition umgesetzt
- Pro-Rata fuer Teilmonate ist fachlich weiterhin offen und deshalb nicht modelliert

## Weitere Spezialpositionen
- `Fahrzeitentschädigung`, `Mehrzeit/Unterzeit`, `Aus- und Weiterbildungskosten inkl. Ferienentschädigung` und `Unfalltaggeld` sind im Excel sichtbar
- diese Positionen werden fachlich noch nicht automatisch in der Payroll-Ableitung verarbeitet
- sie sind Kandidaten für eigene spätere Payroll-Line-Typen, nicht automatisch Zuschlagsarten

## Quellensteuer
- mehrere Tarife pro Kanton und Tarifcode möglich
- Tarife sind zeitlich versionierbar
- Mitarbeiterbezug ist getrennt von der Tarifdefinition

## Vertragshistorie
- `EmploymentContract` ist fachlich als versionierbarer Vertragsstand zu behandeln
- mehrere Vertragsversionen pro Mitarbeitendem sind zulaessig, sofern ihre Gueltigkeitszeiträume fachlich konsistent bleiben
- die Bearbeitung des aktuellsten Vertragsstands in der UI ist nur ein Zwischenstand, nicht das Zielmodell
- offene fachliche Regel:
  - ob sich Vertragsperioden strikt nicht ueberlappen duerfen oder ob definierte Ablösezeitpunkte erlaubt sind, muss vor breiter Umsetzung explizit festgelegt werden

## Payroll-Orchestrierung
- die Application-Schicht orchestriert spaeter gueltigen Vertrag, verdichtete Arbeitsdaten, Spesen und Fahrzeugentschaedigung
- die fachliche Berechnung einzelner Lohnlinien bleibt im Domain-Service `PayrollRunLineDerivationService`
- offene Excel-Spezialpositionen werden nicht spekulativ automatisiert
- wenn Regeln fehlen, muss die Orchestrierung offene Punkte sichtbar weiterreichen statt Ersatzlogik zu erfinden

## Secplan
- Rohformat ist kein Domain-Thema
- Domain übernimmt nur normalisierte Zeitdaten
- Formatmapping bleibt konfigurierbare Integrationslogik
- manueller Erfassungspfad kommt zuerst; spaetere Importlogik soll denselben Zielzustand befuellen

## Regeln
- keine hardcodierten Werte
- versionierbar
- offene oder fehlende Payroll-Regeln werden explizit markiert statt stillschweigend angenommen
