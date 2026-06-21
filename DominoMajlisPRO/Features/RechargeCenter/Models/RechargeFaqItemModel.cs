using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeFaqItemModel : INotifyPropertyChanged
{
    private bool _isExpanded;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
        }
    }
    public string Chevron => IsExpanded ? "⌃" : "⌄";
    public event PropertyChangedEventHandler? PropertyChanged;
}
