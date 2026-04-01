using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoundSwitcher.ViewModels;

public sealed class DeviceSelectionItem : INotifyPropertyChanged
{
    private bool _isIncluded;
    private string _iconKind = Models.TrayIconKinds.DeviceVolume;

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

    public string IconKind
    {
        get => _iconKind;
        set
        {
            if (_iconKind == value)
            {
                return;
            }

            _iconKind = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
