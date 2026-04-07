# UI Design System

## Ziel
Konsistentes Layout und Verhalten.

## Zentrale Style-Bereiche

- App allgemein
  - Markenname
  - optionale Logo-Konfiguration
  - Grundschrift
  - Standard-Schriftgroessen
  - App-Hintergrund und Standard-Textfarbe
- Navigation
  - aktive Navigation
  - inaktive Navigation
  - Navigationsflaechen und Rahmen
- Seitenueberschriften
  - App-Titel
  - Seitenkopf
  - einleitende Beschreibungstexte
- Bereichsueberschriften
  - Card-/Section-Header
  - kleinere Feld- und Gruppenbeschriftungen
- Formulare / Eingabefelder
  - Oberflaechenfarben
  - Card-/Panel-Stile
  - visuelle Gruppierung von Eingabebereichen
- Tabellen / Listen
  - ruhige Standardflaechen
  - hervorgehobene Summen-/Statusbereiche
- Status / Hinweise / Meldungen
  - neutrale Hinweistexte
  - Status-/Warnflaechen
- Buttons / Aktionen
  - primaere Aktionen
  - destruktive Aktionen
  - Platzhalter-/Sekundaeraktionen
- Druck / Print
  - eigene Print-Fontfamilie
  - eigene Print-Schriftgroessen
  - eigene Print-Farben
  - eigenes Logo-/Branding-Token fuer Ausdrucke

## Regeln

- einheitliche Abstände
- einheitliche Typografie
- einheitliche Farben
- gleiche Komponenten überall
- Bildschirm- und Druckstil werden getrennt vorbereitet und nicht hart gekoppelt
- Logo, Brand-Text und spaetere Report-Stile werden zentral statt direkt in Views gepflegt

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

## Aktueller technischer Schnitt

- globale Screen-/App-Ressourcen liegen zentral in `src/Payroll.Desktop/Styles/DesignSystem.axaml`
- Print-/Report-Ressourcen liegen zentral in `src/Payroll.Desktop/Styles/PrintDesignSystem.axaml`
- `App.axaml` bindet diese Style-Dateien zentral ein
- Views sollen bevorzugt mit semantischen Klassen und DynamicResources arbeiten statt mit verstreuten Einzelwerten
