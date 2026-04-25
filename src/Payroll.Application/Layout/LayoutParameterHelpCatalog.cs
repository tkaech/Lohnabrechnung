namespace Payroll.Application.Layout;

public static class LayoutParameterHelpCatalog
{
    private static readonly LayoutParameterHelpGroupDto[] Groups =
    [
        new(
            "spacing",
            "Abstaende",
            "Diese Parameter steuern die Freiraeume zwischen Seitenrand, Panels und inhaltlichen Sektionen.",
            [
                new LayoutParameterHelpItemDto(
                    "Theme.Layout.PagePadding",
                    "Aeusserer Seitenabstand der Hauptflaeche. Ein hoeherer Wert zieht den gesamten Inhalt weiter vom Fensterrand weg.",
                    "Datei `src/Payroll.Desktop/Styles/DesignSystem.axaml` / `Theme.Layout`",
                    LayoutParameterHelpParameterTypes.Spacing,
                    "20",
                    LayoutParameterHelpPreviewKinds.PagePadding,
                    "Gesamte App-Seite",
                    "Der Abstand zwischen Fensterrand und erstem Inhaltsblock wird groesser oder kleiner.",
                    2m),
                new LayoutParameterHelpItemDto(
                    "Theme.Layout.PanelPadding",
                    "Innenabstand innerhalb von Karten und Oberflaechen. Beeinflusst, wie nah Inhalt an den Panelrand rueckt.",
                    "Datei `src/Payroll.Desktop/Styles/DesignSystem.axaml` / `Theme.Layout`",
                    LayoutParameterHelpParameterTypes.Spacing,
                    "12",
                    LayoutParameterHelpPreviewKinds.PanelPadding,
                    "Panel-Innenraum",
                    "Der Text sitzt weiter innen oder naeher am Kartenrand.",
                    1m),
                new LayoutParameterHelpItemDto(
                    "Theme.Layout.SectionSpacing",
                    "Vertikaler Abstand zwischen logisch getrennten Inhaltsbloecken.",
                    "Datei `src/Payroll.Desktop/Styles/DesignSystem.axaml` / `Theme.Layout`",
                    LayoutParameterHelpParameterTypes.Spacing,
                    "12",
                    LayoutParameterHelpPreviewKinds.SectionSpacing,
                    "Abstaende zwischen Sektionen",
                    "Mehr Abstand trennt Bereiche visuell staerker.",
                    1m),
                new LayoutParameterHelpItemDto(
                    "Theme.Layout.TableCellVerticalPadding",
                    "Vertikaler Innenabstand in tabellarischen Listen- und Grid-Zellen. Steuert die Zeilenverdichtung vergleichbarer Tabellenbereiche global.",
                    "Datei `src/Payroll.Desktop/Styles/DesignSystem.axaml` / `Theme.Layout` sowie `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Spacing,
                    "6",
                    LayoutParameterHelpPreviewKinds.TableCellPadding,
                    "Tabellen- und Listenzeilen",
                    "Hoehere Werte machen Tabellenzeilen luftiger, kleinere Werte verdichten Zeilen und Zellinhalt.",
                    1m)
            ]),
        new(
            "surface-shape",
            "Panel-Form",
            "Diese Parameter bestimmen die Form der Oberflaechen und wie weich oder kantig Karten erscheinen.",
            [
                new LayoutParameterHelpItemDto(
                    "Theme.Layout.PanelCornerRadius",
                    "Abrundung von Karten, Oberflaechen und hervorgehobenen Bloecken.",
                    "Datei `src/Payroll.Desktop/Styles/DesignSystem.axaml` / `Theme.Layout`",
                    LayoutParameterHelpParameterTypes.Size,
                    "8",
                    LayoutParameterHelpPreviewKinds.CornerRadius,
                    "Panel-Ecken",
                    "Hoehere Werte runden die Kartenform sichtbar staerker ab.",
                    1m)
            ]),
        new(
            "typography",
            "Typografie",
            "Diese Parameter beeinflussen die Grundschrift im laufenden Programm und damit Lesbarkeit und visuelle Gewichtung.",
            [
                new LayoutParameterHelpItemDto(
                    "AppFontFamily",
                    "Verwendete Schriftfamilie fuer die App-Oberflaeche.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Text,
                    "Aptos",
                    LayoutParameterHelpPreviewKinds.FontFamily,
                    "App-Text allgemein",
                    "Die Schriftform von Ueberschrift und Fliesstext aendert sich nur in dieser lokalen Vorschau."),
                new LayoutParameterHelpItemDto(
                    "AppFontSize",
                    "Grundgroesse fuer Texte in der App. Wirkt auf Dichte und Lesbarkeit.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Size,
                    "13",
                    LayoutParameterHelpPreviewKinds.FontSize,
                    "Textgroesse",
                    "Groessere Werte machen Text praesenter und verbrauchen mehr Platz.",
                    1m)
            ]),
        new(
            "colors-branding",
            "Farben und Branding",
            "Diese Parameter steuern die visuelle Identitaet der App, aber nicht die Berechnungslogik.",
            [
                new LayoutParameterHelpItemDto(
                    "AppTextColorHex",
                    "Primaere Schriftfarbe fuer normalen Inhalt.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Color,
                    "#FF1A2530",
                    LayoutParameterHelpPreviewKinds.TextColor,
                    "Haupttext",
                    "Die Hauptlesefarbe des Inhalts wechselt nur in dieser Zeile."),
                new LayoutParameterHelpItemDto(
                    "AppMutedTextColorHex",
                    "Zurueckgenommene Farbe fuer Hinweise, Sekundaertexte und Erklaerungen.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Color,
                    "#FF5F6B7A",
                    LayoutParameterHelpPreviewKinds.MutedTextColor,
                    "Hinweistexte",
                    "Hinweis- und Sekundaertexte werden heller oder dunkler dargestellt."),
                new LayoutParameterHelpItemDto(
                    "AppBackgroundColorHex",
                    "Grundfarbe hinter den Oberflaechen der App.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Color,
                    "#FFF5F7FA",
                    LayoutParameterHelpPreviewKinds.BackgroundColor,
                    "Fensterhintergrund",
                    "Die Flaechenfarbe des App-Hintergrunds aendert sich in der lokalen Vorschau."),
                new LayoutParameterHelpItemDto(
                    "AppAccentColorHex",
                    "Akzentfarbe fuer hervorgehobene Bereiche und visuelle Leitlinien.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Color,
                    "#FF14324A",
                    LayoutParameterHelpPreviewKinds.AccentColor,
                    "Akzente und Hervorhebungen",
                    "Akzentflaechen und Branding-Hervorhebungen wechseln auf den lokalen Testwert."),
                new LayoutParameterHelpItemDto(
                    "AppLogoText",
                    "Textbasiertes Branding im Kopf, falls kein Bildlogo verwendet wird.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Text,
                    "PA",
                    LayoutParameterHelpPreviewKinds.LogoText,
                    "Logo-/Brandbereich",
                    "Nur das Textlogo in dieser Vorschauzeile wird ersetzt."),
                new LayoutParameterHelpItemDto(
                    "AppLogoPath",
                    "Optionaler Dateipfad zu einem Bildlogo fuer den Kopfbereich.",
                    "Bereich `Einstellungen > Layout > App allgemein`",
                    LayoutParameterHelpParameterTypes.Path,
                    "/pfad/zum/logo.png",
                    LayoutParameterHelpPreviewKinds.LogoPath,
                    "Logo-Bild",
                    "Die Vorschau zeigt nur den lokalen Beispielpfad, ohne das Bild global zu laden.")
            ])
    ];

    public static IReadOnlyList<LayoutParameterHelpGroupDto> GetGroups()
    {
        return Groups
            .Select(group => new LayoutParameterHelpGroupDto(
                group.Key,
                group.Title,
                group.Summary,
                group.Parameters
                    .Select(parameter => new LayoutParameterHelpItemDto(
                        parameter.Name,
                        parameter.Description,
                        parameter.Source,
                        parameter.ParameterType,
                        parameter.ExampleValue,
                        parameter.PreviewKind,
                        parameter.PreviewTarget,
                        parameter.PreviewExplanation,
                        parameter.Step))
                    .ToArray()))
            .ToArray();
    }
}
