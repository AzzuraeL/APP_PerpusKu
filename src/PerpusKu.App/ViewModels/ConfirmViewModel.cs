using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class ConfirmViewModel : DialogViewModelBase
{
    public string Message { get; }
    public string ConfirmText { get; }
    public bool IsDanger { get; }
    public RelayCommand ConfirmCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ConfirmViewModel(string title, string message, string confirmText, bool danger)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        IsDanger = danger;
        ConfirmCommand = new RelayCommand(() => Close(true));
        CancelCommand = new RelayCommand(() => Close(false));
    }
}
