using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Payroll.Desktop.Controls;

public sealed class MonthPicker : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MonthPicker, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
        AvaloniaProperty.Register<MonthPicker, DateTimeOffset?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<MonthPicker, string?>(nameof(Watermark));

    private readonly TextBox _textBox;
    private readonly Button _iconButton;
    private readonly Popup _popup;
    private readonly TextBlock _yearText;
    private readonly List<Button> _monthButtons = [];
    private bool _updating;
    private bool _closeCommitted;
    private bool _openQueued;
    private bool _suppressNextFocusOpen;
    private int _openRequestVersion;
    private string _committedText = string.Empty;
    private int _displayYear = DateTime.Today.Year;
    private DateTimeOffset? _textMonth;

    public MonthPicker()
    {
        _textBox = new TextBox();
        _textBox.Classes.Add("month-picker-input");
        _textBox.AddHandler(
            PointerPressedEvent,
            OnTextBoxPointerPressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        _textBox.GotFocus += OnTextBoxGotFocus;
        _textBox.AddHandler(
            PointerReleasedEvent,
            OnTextBoxPointerReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        _textBox.KeyDown += OnTextBoxKeyDown;

        _iconButton = new Button
        {
            Content = new PathIcon
            {
                Width = 14,
                Height = 14,
                Data = Geometry.Parse("M7 2H9V4H15V2H17V4H20V20H4V4H7V2M6 8V18H18V8H6Z")
            },
            Classes = { "date-picker-icon-button" },
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _iconButton.AddHandler(
            PointerReleasedEvent,
            OnTextBoxPointerReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);

        _yearText = new TextBlock
        {
            Classes = { "month-picker-year" },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _popup = new Popup
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = _textBox,
            IsLightDismissEnabled = true,
            Child = CreatePopupContent()
        };
        _popup.Closed += OnPopupClosed;

        var inputGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        inputGrid.Children.Add(_textBox);
        Grid.SetColumn(_iconButton, 1);
        inputGrid.Children.Add(_iconButton);

        var host = new Grid();
        host.Children.Add(inputGrid);
        host.Children.Add(_popup);
        Content = host;

        UpdateFromSelectedDate();
        UpdateYearText();
        UpdateMonthSelection();
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public DateTimeOffset? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (_updating)
        {
            return;
        }

        if (change.Property == TextProperty)
        {
            var text = Text ?? string.Empty;
            _textBox.Text = text;
            _committedText = text;
            if (TryParseMonth(text, out var parsedMonth))
            {
                _textMonth = parsedMonth;
                _displayYear = parsedMonth.Year;
                UpdateYearText();
                UpdateMonthSelection();
            }
            else
            {
                _textMonth = null;
                UpdateMonthSelection();
            }
        }
        else if (change.Property == SelectedDateProperty)
        {
            UpdateFromSelectedDate();
        }
        else if (change.Property == WatermarkProperty)
        {
            _textBox.Watermark = Watermark;
        }
    }

    private Control CreatePopupContent()
    {
        var previousYearButton = new Button { Content = "<", Classes = { "month-picker-nav-button" } };
        previousYearButton.Click += (_, _) => ChangeDisplayYear(-1);

        var nextYearButton = new Button { Content = ">", Classes = { "month-picker-nav-button" } };
        nextYearButton.Click += (_, _) => ChangeDisplayYear(1);

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        header.Children.Add(previousYearButton);
        Grid.SetColumn(_yearText, 1);
        header.Children.Add(_yearText);
        Grid.SetColumn(nextYearButton, 2);
        header.Children.Add(nextYearButton);

        var monthsGrid = new UniformGrid
        {
            Columns = 3,
            Rows = 4
        };

        var monthNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;
        for (var month = 1; month <= 12; month++)
        {
            var monthButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(monthNames[month - 1])
                    ? month.ToString("00", CultureInfo.InvariantCulture)
                    : monthNames[month - 1],
                Tag = month,
                Classes = { "month-picker-month-button" }
            };
            monthButton.Click += OnMonthButtonClick;
            _monthButtons.Add(monthButton);
            monthsGrid.Children.Add(monthButton);
        }

        var panel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                header,
                monthsGrid
            }
        };

        var border = new Border
        {
            Classes = { "month-picker-popup" },
            Child = panel
        };

        return border;
    }

    private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (_suppressNextFocusOpen)
        {
            _suppressNextFocusOpen = false;
            return;
        }

        QueueOpenPopup();
    }

    private void OnTextBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_textBox);
        _suppressNextFocusOpen = !point.Properties.IsLeftButtonPressed;
    }

    private void OnTextBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var point = e.GetCurrentPoint(_textBox);
        if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
        {
            return;
        }

        _textBox.Focus();
        QueueOpenPopup();
        e.Handled = true;
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _closeCommitted = CommitManualText();
            ClosePopup();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            RevertText();
            if (_popup.IsOpen)
            {
                _closeCommitted = true;
            }
            ClosePopup();
            e.Handled = true;
        }
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _openQueued = false;
        _openRequestVersion++;

        if (_closeCommitted)
        {
            _closeCommitted = false;
            return;
        }

        RevertText();
    }

    private void QueueOpenPopup()
    {
        if (_popup.IsOpen || _openQueued)
        {
            return;
        }

        _openQueued = true;
        var requestVersion = ++_openRequestVersion;
        Dispatcher.UIThread.Post(() =>
        {
            if (requestVersion != _openRequestVersion)
            {
                return;
            }

            _openQueued = false;
            OpenPopup();
        });
    }

    private void OpenPopup()
    {
        if (_popup.IsOpen)
        {
            return;
        }

        _closeCommitted = false;
        _committedText = Text ?? _textBox.Text ?? string.Empty;

        if (TryParseMonth(_textBox.Text, out var parsedMonth))
        {
            _displayYear = parsedMonth.Year;
        }
        else if (SelectedDate.HasValue)
        {
            _displayYear = SelectedDate.Value.Year;
        }

        UpdateYearText();
        UpdateMonthSelection();
        _popup.IsOpen = true;
    }

    private void ClosePopup()
    {
        _openRequestVersion++;
        _openQueued = false;
        if (_popup.IsOpen)
        {
            _popup.IsOpen = false;
            return;
        }

        _closeCommitted = false;
    }

    private bool CommitManualText()
    {
        var text = _textBox.Text ?? string.Empty;
        if (!TryParseMonth(text, out var parsedMonth))
        {
            return false;
        }

        ApplyMonth(parsedMonth.Year, parsedMonth.Month);
        return true;
    }

    private void RevertText()
    {
        _updating = true;
        try
        {
            _textBox.Text = _committedText;
            Text = _committedText;
            _textMonth = TryParseMonth(_committedText, out var revertedMonth)
                ? revertedMonth
                : null;
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnMonthButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int month })
        {
            return;
        }

        ApplyMonth(_displayYear, month);
        _closeCommitted = true;
        ClosePopup();
    }

    private void ApplyMonth(int year, int month)
    {
        var normalizedMonth = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var formattedMonth = normalizedMonth.ToString("MM/yyyy", CultureInfo.InvariantCulture);

        _updating = true;
        try
        {
            _textBox.Text = formattedMonth;
            Text = formattedMonth;
            SelectedDate = normalizedMonth;
            _committedText = formattedMonth;
            _displayYear = year;
            _textMonth = normalizedMonth;
        }
        finally
        {
            _updating = false;
        }

        UpdateYearText();
        UpdateMonthSelection();
    }

    private void ChangeDisplayYear(int delta)
    {
        _displayYear += delta;
        UpdateYearText();
        UpdateMonthSelection();
    }

    private void UpdateFromSelectedDate()
    {
        if (!SelectedDate.HasValue)
        {
            return;
        }

        var normalizedMonth = new DateTimeOffset(SelectedDate.Value.Year, SelectedDate.Value.Month, 1, 0, 0, 0, SelectedDate.Value.Offset);
        var formattedMonth = normalizedMonth.ToString("MM/yyyy", CultureInfo.InvariantCulture);

        _updating = true;
        try
        {
            _textBox.Text = formattedMonth;
            Text = formattedMonth;
            _committedText = formattedMonth;
            _displayYear = normalizedMonth.Year;
            _textMonth = normalizedMonth;
        }
        finally
        {
            _updating = false;
        }

        UpdateYearText();
        UpdateMonthSelection();
    }

    private void UpdateYearText()
    {
        _yearText.Text = _displayYear.ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateMonthSelection()
    {
        var selectedDate = SelectedDate ?? _textMonth;
        var selectedMonth = selectedDate.HasValue && selectedDate.Value.Year == _displayYear
            ? selectedDate.Value.Month
            : 0;

        foreach (var monthButton in _monthButtons)
        {
            monthButton.Classes.Set("selected", monthButton.Tag is int month && month == selectedMonth);
        }
    }

    private static bool TryParseMonth(string? value, out DateTimeOffset normalizedMonth)
    {
        normalizedMonth = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var supportedFormats = new[]
        {
            "MM/yyyy",
            "M/yyyy",
            "MM.yyyy",
            "M.yyyy",
            "yyyy-MM",
            "yyyy/MM"
        };

        if (DateTime.TryParseExact(value.Trim(), supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth)
            || DateTime.TryParse(value.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedMonth))
        {
            normalizedMonth = new DateTimeOffset(parsedMonth.Year, parsedMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
            return true;
        }

        return false;
    }
}
