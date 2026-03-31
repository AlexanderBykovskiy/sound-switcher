using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoundSwitcher.ViewModels;

public sealed class DeviceSelectionItem : INotifyPropertyChanged
{
    private bool _isIncluded;

    public required string Id { get; init; }
    public required string Name { get; init; }

    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (_isIncluded == value)
            {
                return;
            }

            _isIncluded = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
