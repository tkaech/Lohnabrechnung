using Avalonia.Threading;

namespace Payroll.Desktop.ViewModels;

public enum ButtonFeedbackState
{
    Neutral,
    Success,
    Error
}

public sealed class ButtonFeedbackViewModel : ViewModelBase
{
    private const int ResetDelayMilliseconds = 1500;
    private int _stateVersion;
    private ButtonFeedbackState _state;

    public ButtonFeedbackState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                RaisePropertyChanged(nameof(IsNeutral));
                RaisePropertyChanged(nameof(IsSuccess));
                RaisePropertyChanged(nameof(IsError));
            }
        }
    }

    public bool IsNeutral => State == ButtonFeedbackState.Neutral;
    public bool IsSuccess => State == ButtonFeedbackState.Success;
    public bool IsError => State == ButtonFeedbackState.Error;

    public void SetSuccess() => SetState(ButtonFeedbackState.Success);

    public void SetError() => SetState(ButtonFeedbackState.Error);

    private void SetState(ButtonFeedbackState state)
    {
        _stateVersion++;
        var currentVersion = _stateVersion;
        State = state;
        _ = ResetAsync(currentVersion);
    }

    private async Task ResetAsync(int stateVersion)
    {
        await Task.Delay(ResetDelayMilliseconds);

        if (stateVersion != _stateVersion)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (stateVersion == _stateVersion)
            {
                State = ButtonFeedbackState.Neutral;
            }
        });
    }
}
