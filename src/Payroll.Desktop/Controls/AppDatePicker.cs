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

public sealed class AppDatePicker : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<AppDatePicker, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
        AvaloniaProperty.Register<AppDatePicker, DateTimeOffset?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<AppDatePicker, string?>(nameof(Watermark));

    private readonly TextBox _textBox;
    private readonly Button _iconButton;
    private readonly Popup _popup;
    private readonly TextBlock _monthYearText;
    private readonly UniformGrid _daysGrid;
    private readonly List<Button> _dayButtons = [];
    private bool _updating;
    private bool _closeCommitted;
    private bool _openQueued;
    private bool _suppressNextFocusOpen;
    private int _openRequestVersion;
    private string _committedText = string.Empty;
    private DateTimeOffset? _committedDate;
    private DateTimeOffset? _draftDate;
    private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public AppDatePicker()
    {
        _textBox = new TextBox();
        _textBox.Classes.Add("date-picker-input");
        _textBox.AddHandler(
            PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        _textBox.GotFocus += OnTextBoxGotFocus;
        _textBox.AddHandler(
            PointerReleasedEvent,
            OnPointerReleased,
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
            OnPointerReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);

        _monthYearText = new TextBlock
        {
            Classes = { "month-picker-year" },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _daysGrid = new UniformGrid
        {
            Columns = 7,
            Rows = 7
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
        RefreshCalendar();
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
            ApplyExternalText(Text ?? string.Empty);
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
        var previousMonthButton = new Button { Content = "<", Classes = { "month-picker-nav-button" } };
        previousMonthButton.Click += (_, _) => ChangeDisplayMonth(-1);

        var nextMonthButton = new Button { Content = ">", Classes = { "month-picker-nav-button" } };
        nextMonthButton.Click += (_, _) => ChangeDisplayMonth(1);

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        header.Children.Add(previousMonthButton);
        Grid.SetColumn(_monthYearText, 1);
        header.Children.Add(_monthYearText);
        Grid.SetColumn(nextMonthButton, 2);
        header.Children.Add(nextMonthButton);

        var actions = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*")
        };
        var todayButton = new Button { Content = "Today", Classes = { "date-picker-action-button" } };
        todayButton.Click += (_, _) => SetDraftDate(DateTime.Today);
        var clearButton = new Button { Content = "Clear", Classes = { "date-picker-action-button" } };
        clearButton.Click += (_, _) => ClearDraftDate();
        actions.Children.Add(todayButton);
        Grid.SetColumn(clearButton, 1);
        actions.Children.Add(clearButton);

        var panel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                header,
                _daysGrid,
                actions
            }
        };

        return new Border
        {
            Classes = { "month-picker-popup" },
            Child = panel
        };
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

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        _suppressNextFocusOpen = !point.Properties.IsLeftButtonPressed;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
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
        _committedText = _textBox.Text ?? string.Empty;
        _committedDate = TryParseDate(_committedText, out var parsedTextDate)
            ? parsedTextDate
            : SelectedDate;
        _draftDate = _committedDate;

        if (_draftDate.HasValue)
        {
            _displayMonth = StartOfMonth(_draftDate.Value.DateTime);
        }

        RefreshCalendar();
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
        if (string.IsNullOrWhiteSpace(text))
        {
            ApplyCommittedDate(null);
            return true;
        }

        if (!TryParseDate(text, out var parsedDate))
        {
            return false;
        }

        ApplyCommittedDate(parsedDate);
        return true;
    }

    private void RevertText()
    {
        _updating = true;
        try
        {
            _textBox.Text = _committedText;
            Text = _committedText;
            _draftDate = _committedDate;
        }
        finally
        {
            _updating = false;
        }

        RefreshCalendar();
    }

    private void ApplyCommittedDate(DateTimeOffset? value)
    {
        var normalizedDate = value.HasValue
            ? NormalizeDate(value.Value)
            : (DateTimeOffset?)null;
        var formattedDate = normalizedDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;

        _updating = true;
        try
        {
            _textBox.Text = formattedDate;
            Text = formattedDate;
            SelectedDate = normalizedDate;
            _committedText = formattedDate;
            _committedDate = normalizedDate;
            _draftDate = normalizedDate;
        }
        finally
        {
            _updating = false;
        }

        if (normalizedDate.HasValue)
        {
            _displayMonth = StartOfMonth(normalizedDate.Value.DateTime);
        }

        RefreshCalendar();
    }

    private void ApplyExternalText(string text)
    {
        _textBox.Text = text;
        _committedText = text;
        if (TryParseDate(text, out var parsedDate))
        {
            _committedDate = parsedDate;
            _draftDate = parsedDate;
            _displayMonth = StartOfMonth(parsedDate.DateTime);
        }
        else if (string.IsNullOrWhiteSpace(text))
        {
            _committedDate = null;
            _draftDate = null;
        }

        RefreshCalendar();
    }

    private void UpdateFromSelectedDate()
    {
        if (!SelectedDate.HasValue)
        {
            ApplyCommittedDate(null);
            return;
        }

        var normalizedDate = NormalizeDate(SelectedDate.Value);
        var formattedDate = normalizedDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        _updating = true;
        try
        {
            _textBox.Text = formattedDate;
            Text = formattedDate;
            _committedText = formattedDate;
            _committedDate = normalizedDate;
            _draftDate = normalizedDate;
            _displayMonth = StartOfMonth(normalizedDate.DateTime);
        }
        finally
        {
            _updating = false;
        }

        RefreshCalendar();
    }

    private void SetDraftDate(DateTime date)
    {
        var normalizedDate = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        _draftDate = normalizedDate;
        _displayMonth = StartOfMonth(date);
        _textBox.Text = normalizedDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        RefreshCalendar();
    }

    private void ClearDraftDate()
    {
        _draftDate = null;
        _textBox.Text = string.Empty;
        RefreshCalendar();
    }

    private void OnDayButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime date })
        {
            return;
        }

        ApplyCommittedDate(new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero));
        _closeCommitted = true;
        ClosePopup();
    }

    private void ChangeDisplayMonth(int delta)
    {
        _displayMonth = _displayMonth.AddMonths(delta);
        RefreshCalendar();
    }

    private void RefreshCalendar()
    {
        _monthYearText.Text = _displayMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        _daysGrid.Children.Clear();
        _dayButtons.Clear();

        var dayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        for (var i = 1; i <= 7; i++)
        {
            var dayOfWeek = (DayOfWeek)(i % 7);
            _daysGrid.Children.Add(new TextBlock
            {
                Text = dayNames[(int)dayOfWeek],
                Classes = { "date-picker-weekday" },
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        var firstDayOffset = ((int)_displayMonth.DayOfWeek + 6) % 7;
        var firstVisibleDate = _displayMonth.AddDays(-firstDayOffset);
        var today = DateTime.Today;

        for (var i = 0; i < 42; i++)
        {
            var date = firstVisibleDate.AddDays(i);
            var button = new Button
            {
                Content = date.Day.ToString(CultureInfo.InvariantCulture),
                Tag = date,
                Classes = { "date-picker-day-button" }
            };

            button.Classes.Set("muted", date.Month != _displayMonth.Month);
            button.Classes.Set("today", date.Date == today);
            button.Classes.Set("selected", _draftDate.HasValue && _draftDate.Value.Date == date.Date);
            button.Click += OnDayButtonClick;
            _dayButtons.Add(button);
            _daysGrid.Children.Add(button);
        }
    }

    private static DateTime StartOfMonth(DateTime value)
    {
        return new DateTime(value.Year, value.Month, 1);
    }

    private static DateTimeOffset NormalizeDate(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, value.Offset);
    }

    private static bool TryParseDate(string? value, out DateTimeOffset normalizedDate)
    {
        normalizedDate = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var supportedFormats = new[]
        {
            "dd.MM.yyyy",
            "d.M.yyyy",
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "d/M/yyyy"
        };

        if (DateTime.TryParseExact(value.Trim(), supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            || DateTime.TryParse(value.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate))
        {
            normalizedDate = new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, TimeSpan.Zero);
            return true;
        }

        return false;
    }
}
