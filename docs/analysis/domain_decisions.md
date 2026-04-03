# Domain Decisions

## Stunden
- werden immer als decimal gespeichert

## Zuschläge
- werden als Stunden gespeichert
- Berechnung erfolgt im Payroll-Modul
- Prozentwerte oder Bewertungsfaktoren gehören zu versionierbaren Payroll-Regeln, nicht zu `TimeEntry`
- Nacht-, Sonntags- und Feiertagsstunden werden als eigene fachliche Mengen behandelt
- fehlende Zuschlagsregeln werden nicht implizit berechnet

## Spesen
- immer CHF
- mehrere Spesenarten sind fachlich vorgesehen
- Fahrzeugentschädigung ist davon getrennt

## Fahrzeugentschädigung
- zentral erfassen
- nicht pro Formular

## BVG
- fixer CHF-Betrag
- nicht prozentual
- pro Mitarbeitendem und gültigem Arbeitsvertrag festgelegt
- wird im Lohnlauf als fixe Abzugsposition umgesetzt

## Quellensteuer
- mehrere Tarife pro Kanton und Tarifcode möglich
- Tarife sind zeitlich versionierbar
- Mitarbeiterbezug ist getrennt von der Tarifdefinition

## Secplan
- Rohformat ist kein Domain-Thema
- Domain übernimmt nur normalisierte Zeitdaten
- Formatmapping bleibt konfigurierbare Integrationslogik

## Regeln
- keine hardcodierten Werte
- versionierbar
- offene oder fehlende Payroll-Regeln werden explizit markiert statt stillschweigend angenommen
