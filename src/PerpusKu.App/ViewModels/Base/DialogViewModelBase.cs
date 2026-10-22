namespace PerpusKu.ViewModels.Base;

public abstract class DialogViewModelBase : ViewModelBase
{
    public string Title { get; protected set; }
    public event Action<bool?> CloseRequested;
    protected void Close(bool? result) => CloseRequested?.Invoke(result);
}
