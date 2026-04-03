# UI Design System

## Ziel
Konsistentes Layout und Verhalten.

## Regeln

- einheitliche Abstände
- einheitliche Typografie
- einheitliche Farben
- gleiche Komponenten überall

## Standard Controls

- AppButton
- AppTextBox
- AppNumberBox
- AppDatePicker
- AppComboBox
- AppDataGrid
- AppDialog
- AppFormField
- AppSectionCard

## Arbeitsoberflaeche

- Module werden ueber eine stabile Navigationsleiste bzw. einen Bereichs-Sidebar vorbereitet
- Detailmasken arbeiten standardmaessig im Ansichtsmodus
- Bearbeitung startet nur explizit ueber `Bearbeiten` oder `Neu`
- Aktionsleiste ist konsistent:
  - links fuer Kontextaktionen wie `Neu`, `Bearbeiten`, `Loeschen`
  - rechts fuer editierbezogene Aktionen wie `Abbrechen`, `Speichern`
- Sicherheitskritische Aktionen werden nicht sofort ausgefuehrt, sondern ueber klare Bestätigung im UI abgesichert

## Monatserfassung Zeiten Und Spesen

- naechster groesserer Arbeitsbereich ist die gemeinsame Monatserfassung pro Mitarbeitenden
- Kopfbereich der kuenftigen Arbeitsflaeche zeigt:
  - Mitarbeitender
  - Monat
  - Vertragsstand fuer den Monat
  - Status der Erfassung
  - kompakte Monatssummen
- darunter sind drei Register vorbereitet:
  - `Zeiten`
  - `Spesen`
  - `Monatsvorschau`
- `Zeiten`
  - einfache manuelle Tageserfassung je Datum im ersten Schritt
  - keine komplexe Kommen-/Gehen-Logik im ersten Schritt
  - Monatsuebersicht und Summen gehoeren dazu
- `Spesen`
  - tabellarische Erfassung im selben Monatskontext
  - Fahrzeugentschaedigung bleibt fachlich getrennt und wird als eigener Unterbereich beschrieben
- `Monatsvorschau`
  - nur Verdichtung
  - keine Rohdatenerfassung
  - zeigt Monatsresultate, vorlaeufige Lohnpositionen und fachliche Hinweise

## Prinzip

Keine individuellen Lösungen wenn wiederverwendbare Komponenten möglich sind.
