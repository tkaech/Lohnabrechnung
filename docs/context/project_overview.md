# Projektübersicht

## Projektname
PayrollApp

## Ziel
Entwicklung einer Desktop-Anwendung für Lohnverwaltung in der Schweiz für eine Firma mit ca. 40 Mitarbeitenden.

## Technischer Stack
- Sprache: C#
- Framework: .NET 8
- UI: Avalonia
- IDE: Visual Studio Code
- Datenbank: SQLite (Start), später PostgreSQL
- ORM: Entity Framework Core

## Ausgangslage
Die aktuelle Lohnverwaltung basiert auf einer Excel-Datei:
- Tab 1: Übersicht aller Mitarbeitenden
- Weitere Tabs: individuelles Lohnblatt pro Mitarbeitenden

Das Excel dient als fachliche Grundlage, wird aber nicht technisch übernommen.

## Hauptfunktionen
- Mitarbeiterverwaltung
- Import von Arbeitszeitdaten (Secplan)
- Zeiterfassung (Stunden, Pausen, Überzeit)
- Spesenverwaltung
- Zuschläge
- Lohnberechnung
- Sozialversicherungen (AHV, ALV, UVG, BVG)
- Quellensteuer
- Grenzgänger / Bewilligungen
- Lohnausweis / Jahresabschluss
- Audit / Revision / Protokolle

## Fachliche Prinzipien
- Schweizer Recht berücksichtigen
- Regeln und Sätze sind konfigurierbar und versionierbar
- Keine hardcodierten Werte
- Manuelle Anpassungen sind auditierbar
- Importdaten sind nachvollziehbar

## Nicht-Ziele (MVP)
- Keine Swissdec-Zertifizierung
- Kein Mehrbenutzerbetrieb